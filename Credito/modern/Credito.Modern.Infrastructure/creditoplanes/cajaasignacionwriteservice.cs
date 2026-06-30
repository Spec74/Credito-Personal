using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CajaAsignacionWriteService(IOptions<SqlDatabaseOptions> options) : ICajaAsignacionWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<(string? Error, AsignarCajaResponse? Result)> AsignarAsync(
        int oficinaId,
        int cajaId,
        decimal saldoInicial,
        int usuarioRegId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (cajaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaId), "cajaId debe ser >= 1.");
        }

        if (usuarioRegId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioRegId), "usuarioRegId debe ser >= 1.");
        }

        if (saldoInicial < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(saldoInicial), "saldoInicial debe ser >= 0.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var caja = await connection.QueryFirstOrDefaultAsync<CajaAsignacionRow>(
                new CommandDefinition(
                    """
                    SELECT CajaId, OficinaId, Denominacion, Estado, IndAbierto, CajeroId
                    FROM CREDITO.Caja WITH (UPDLOCK, HOLDLOCK)
                    WHERE CajaId = @CajaId;
                    """,
                    new { CajaId = cajaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (caja is null || caja.OficinaId != oficinaId)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("No existe la caja indicada en la oficina.", null);
            }

            if (!caja.Estado || caja.IndAbierto)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("La caja no está disponible para asignación.", null);
            }

            if (caja.CajeroId is null or < 1)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("No tiene adignado un Cajero.", null);
            }

            var esEncargado = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT CAST(CASE WHEN EXISTS (
                        SELECT 1
                        FROM MAESTRO.UsuarioRol AS ur
                        INNER JOIN MAESTRO.Rol AS r ON r.RolId = ur.RolId
                        WHERE ur.UsuarioId = @UsuarioId
                          AND ur.OficinaId = @OficinaId
                          AND r.Denominacion = N'ENCARGADO'
                    ) THEN 1 ELSE 0 END AS bit);
                    """,
                    new { UsuarioId = usuarioRegId, OficinaId = oficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var boveda = await connection.QueryFirstOrDefaultAsync<BovedaSaldoRow>(
                new CommandDefinition(
                    """
                    SELECT BovedaId, SaldoInicial, SaldoFinal
                    FROM CREDITO.Boveda WITH (UPDLOCK, HOLDLOCK)
                    WHERE OficinaId = @OficinaId
                      AND IndCierre = CAST(0 AS bit)
                      AND IndTemporal = @IndTemporal;
                    """,
                    new { OficinaId = oficinaId, IndTemporal = esEncargado },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (boveda is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("No existe bóveda abierta para la oficina.", null);
            }

            if (saldoInicial > boveda.SaldoFinal)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("Saldo Insuficiente de la boveda.", null);
            }

            var esCajaChica = caja.Denominacion.Contains("CAJA CHICA", StringComparison.OrdinalIgnoreCase);
            int? cajaDiarioId = null;

            if (!esCajaChica)
            {
                cajaDiarioId = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO CREDITO.CajaDiario (
                            CajaId,
                            UsuarioAsignadoId,
                            SaldoInicial,
                            Entradas,
                            Salidas,
                            SaldoFinal,
                            FechaIniOperacion,
                            IndCierre,
                            TransBoveda,
                            MontoPorCobrar,
                            MontoCobrado,
                            SaldoCartera,
                            NroClientesSaldoCartera,
                            SaldoMoraCartera,
                            NroClientesSaldoMoraCartera,
                            NroClientesNuevos)
                        OUTPUT INSERTED.CajaDiarioId
                        VALUES (
                            @CajaId,
                            @UsuarioAsignadoId,
                            @SaldoInicial,
                            0,
                            0,
                            @SaldoInicial,
                            @Fecha,
                            CAST(0 AS bit),
                            CAST(0 AS bit),
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0);
                        """,
                        new
                        {
                            CajaId = cajaId,
                            UsuarioAsignadoId = caja.CajeroId.Value,
                            SaldoInicial = saldoInicial,
                            Fecha = fechaOperacion,
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                var calcParams = new DynamicParameters(new { UsuarioId = caja.CajeroId.Value });
                calcParams.Add("ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "CREDITO.usp_CalcularMontoPorCobrar",
                        calcParams,
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        SET IDENTITY_INSERT CREDITO.CajaChicaDiario ON;
                        INSERT INTO CREDITO.CajaChicaDiario (
                            Id,
                            UsuarioId,
                            SaldoInicial,
                            Entradas,
                            Salidas,
                            SaldoFinal,
                            FechaIniOperacion,
                            IndCierre,
                            TransBoveda)
                        VALUES (
                            @Id,
                            @UsuarioId,
                            @SaldoInicial,
                            0,
                            0,
                            @SaldoInicial,
                            @Fecha,
                            CAST(0 AS bit),
                            CAST(0 AS bit));
                        SET IDENTITY_INSERT CREDITO.CajaChicaDiario OFF;
                        """,
                        new
                        {
                            Id = cajaId,
                            UsuarioId = caja.CajeroId.Value,
                            SaldoInicial = saldoInicial,
                            Fecha = fechaOperacion,
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            var cajasAbiertas = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.Caja
                    SET IndAbierto = CAST(1 AS bit)
                    WHERE CajaId = @CajaId
                      AND IndAbierto = CAST(0 AS bit);
                    """,
                    new { CajaId = cajaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (cajasAbiertas != 1)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("La caja no está disponible para asignación.", null);
            }

            if (saldoInicial > 0)
            {
                var glosa = $"INICIAL {caja.Denominacion} {fechaOperacion:d}";
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO CREDITO.BovedaMov (
                            BovedaId,
                            CodOperacion,
                            TipoPagoId,
                            Glosa,
                            Importe,
                            IndEntrada,
                            Estado,
                            CajaDiarioId,
                            UsuarioRegId,
                            FechaReg)
                        VALUES (
                            @BovedaId,
                            N'TRS',
                            1,
                            @Glosa,
                            @Importe,
                            CAST(0 AS bit),
                            CAST(1 AS bit),
                            @CajaDiarioId,
                            @UsuarioRegId,
                            @Fecha);
                        """,
                        new
                        {
                            BovedaId = boveda.BovedaId,
                            Glosa = glosa,
                            Importe = saldoInicial,
                            CajaDiarioId = esCajaChica ? (int?)null : cajaDiarioId,
                            UsuarioRegId = usuarioRegId,
                            Fecha = fechaOperacion,
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                var totales = await connection.QueryFirstAsync<BovedaTotalesRow>(
                    new CommandDefinition(
                        """
                        SELECT
                            ISNULL(SUM(CASE WHEN IndEntrada = CAST(1 AS bit) THEN Importe ELSE 0 END), 0) AS Entradas,
                            ISNULL(SUM(CASE WHEN IndEntrada = CAST(0 AS bit) THEN Importe ELSE 0 END), 0) AS Salidas
                        FROM CREDITO.BovedaMov WITH (UPDLOCK, HOLDLOCK)
                        WHERE BovedaId = @BovedaId
                          AND Estado = CAST(1 AS bit);
                        """,
                        new { BovedaId = boveda.BovedaId },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE CREDITO.Boveda
                        SET Entradas = @Entradas,
                            Salidas = @Salidas,
                            SaldoFinal = SaldoInicial + @Entradas - @Salidas
                        WHERE BovedaId = @BovedaId;
                        """,
                        new
                        {
                            BovedaId = boveda.BovedaId,
                            totales.Entradas,
                            totales.Salidas,
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return (null, new AsignarCajaResponse(cajaDiarioId, esCajaChica));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private sealed class CajaAsignacionRow
    {
        public int CajaId { get; init; }
        public int OficinaId { get; init; }
        public string Denominacion { get; init; } = string.Empty;
        public bool Estado { get; init; }
        public bool IndAbierto { get; init; }
        public int? CajeroId { get; init; }
    }

    private sealed class BovedaSaldoRow
    {
        public int BovedaId { get; init; }
        public decimal SaldoInicial { get; init; }
        public decimal SaldoFinal { get; init; }
    }

    private sealed class BovedaTotalesRow
    {
        public decimal Entradas { get; init; }
        public decimal Salidas { get; init; }
    }
}
