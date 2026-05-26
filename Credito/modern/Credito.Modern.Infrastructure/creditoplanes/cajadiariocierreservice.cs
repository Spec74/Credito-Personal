using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CajaDiarioCierreService(
    IOptions<SqlDatabaseOptions> options,
    ICompletarImpagosValidacionReadService impagosValidacion) : ICajaDiarioCierreService
{
    private const string SelectCajaDiarioSql = """
        SELECT cd.CajaDiarioId,
               cd.CajaId,
               cd.SaldoFinal,
               cd.IndCierre,
               cd.UsuarioAsignadoId
        FROM CREDITO.CajaDiario AS cd
        WHERE cd.CajaDiarioId = @CajaDiarioId;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<ValidarCierreCajaDiarioResponse> ValidarCierreAsync(
        int cajaDiarioId,
        CancellationToken cancellationToken = default)
    {
        var info = await GetCajaDiarioInfoAsync(cajaDiarioId, cancellationToken).ConfigureAwait(false);
        if (info is null)
        {
            throw new KeyNotFoundException($"No existe caja diario {cajaDiarioId}.");
        }

        if (info.IndCierre)
        {
            return new ValidarCierreCajaDiarioResponse(false, ["La caja diario ya está cerrada."]);
        }

        var blockers = new List<string>();
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var cobranzasPendientes = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CuentaxCobrar AS cxc
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = cxc.CreditoId
                WHERE cxc.Estado = 'PEN'
                  AND c.UsuarioRegId = @UsuarioAsignadoId;
                """,
                new { info.UsuarioAsignadoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (cobranzasPendientes > 0)
        {
            blockers.Add("Tiene Cobranzas Pendientes");
        }

        var desembolsosPendientes = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.Credito AS c
                WHERE c.Estado = 'APR'
                  AND c.UsuarioRegId = @UsuarioAsignadoId;
                """,
                new { info.UsuarioAsignadoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (desembolsosPendientes > 0)
        {
            blockers.Add("Tiene Desembolsos pendientes");
        }

        var creditosPorAprobar = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.Credito AS c
                WHERE c.Estado = 'PEN'
                  AND c.UsuarioRegId = @UsuarioAsignadoId;
                """,
                new { info.UsuarioAsignadoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (creditosPorAprobar > 0)
        {
            blockers.Add("Tiene Creditos por Aprobar Pendientes");
        }

        var impagos = await impagosValidacion.ValidarAsync(cajaDiarioId, cancellationToken).ConfigureAwait(false);
        if (impagos.CantidadImpagosPendientes is > 0)
        {
            blockers.Add("Tiene Creditos Impagos Pendientes");
        }

        var pagosNoVerificados = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.MovimientoCaja AS mc
                INNER JOIN CREDITO.MovimientoCajaExtension AS e ON e.MovimientoCajaId = mc.MovimientoCajaId
                WHERE mc.CajaDiarioId = @CajaDiarioId
                  AND mc.Estado = CAST(1 AS bit)
                  AND mc.TipoPagoId > 1
                  AND e.IndTransferenciaVerificada = CAST(0 AS bit);
                """,
                new { CajaDiarioId = cajaDiarioId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (pagosNoVerificados > 0)
        {
            blockers.Add("Tiene Pagos no Verificados Yape, Plin o transferencias Pendientes");
        }

        return new ValidarCierreCajaDiarioResponse(blockers.Count == 0, blockers);
    }

    public async Task<CerrarCajaDiarioResponse> CerrarAsync(
        int cajaDiarioId,
        int usuarioModId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default)
    {
        if (usuarioModId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioModId), "usuarioModId debe ser >= 1.");
        }

        var info = await GetCajaDiarioInfoAsync(cajaDiarioId, cancellationToken).ConfigureAwait(false);
        if (info is null)
        {
            throw new KeyNotFoundException($"No existe caja diario {cajaDiarioId}.");
        }

        if (info.IndCierre)
        {
            throw new InvalidOperationException("La caja diario ya está cerrada.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var montoCobrado = await connection.ExecuteScalarAsync<decimal>(
                new CommandDefinition(
                    """
                    SELECT ISNULL(SUM(mc.ImportePago), 0)
                    FROM CREDITO.MovimientoCaja AS mc
                    WHERE mc.CajaDiarioId = @CajaDiarioId
                      AND mc.IndEntrada = CAST(1 AS bit)
                      AND mc.Operacion = 'CUO'
                      AND mc.Estado = CAST(1 AS bit);
                    """,
                    new { CajaDiarioId = cajaDiarioId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var saldoFinalRedondeado = Math.Round(info.SaldoFinal, 1, MidpointRounding.AwayFromZero);

            var filasCajaDiario = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.CajaDiario
                    SET IndCierre = CAST(1 AS bit),
                        SaldoFinal = @SaldoFinal,
                        FechaFinOperacion = @FechaFinOperacion,
                        MontoCobrado = @MontoCobrado
                    WHERE CajaDiarioId = @CajaDiarioId
                      AND IndCierre = CAST(0 AS bit);
                    """,
                    new
                    {
                        CajaDiarioId = cajaDiarioId,
                        SaldoFinal = saldoFinalRedondeado,
                        FechaFinOperacion = fechaOperacion,
                        MontoCobrado = montoCobrado,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (filasCajaDiario == 0)
            {
                throw new InvalidOperationException("No se pudo cerrar la caja diario (ya cerrada o no encontrada).");
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.Caja
                    SET IndAbierto = CAST(0 AS bit),
                        FechaMod = @FechaMod,
                        UsuarioModId = @UsuarioModId
                    WHERE CajaId = @CajaId;
                    """,
                    new
                    {
                        CajaId = info.CajaId,
                        FechaMod = fechaOperacion,
                        UsuarioModId = usuarioModId,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "CREDITO.ActualizarClientesNuevos",
                    new { CajaDiarioId = cajaDiarioId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CerrarCajaDiarioResponse(cajaDiarioId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<CajaDiarioCierreInfo?> GetCajaDiarioInfoAsync(
        int cajaDiarioId,
        CancellationToken cancellationToken)
    {
        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<CajaDiarioCierreInfo>(
            new CommandDefinition(
                SelectCajaDiarioSql,
                new { CajaDiarioId = cajaDiarioId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private sealed class CajaDiarioCierreInfo
    {
        public int CajaDiarioId { get; init; }
        public int CajaId { get; init; }
        public decimal SaldoFinal { get; init; }
        public bool IndCierre { get; init; }
        public int UsuarioAsignadoId { get; init; }
    }
}
