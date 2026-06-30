using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CajaChicaDiarioWriteService(IOptions<SqlDatabaseOptions> options)
    : ICajaChicaDiarioWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<(string? Error, TransferirCierreCajaChicaResponse? Result)> TransferirCierreABovedaAsync(
        int oficinaId,
        int usuarioRegId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || usuarioRegId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "Parámetros inválidos.");
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
            var bovedaId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT TOP 1 BovedaId
                    FROM CREDITO.Boveda
                    WHERE OficinaId = @OficinaId
                      AND IndCierre = CAST(0 AS bit)
                    ORDER BY IndTemporal, BovedaId;
                    """,
                    new { OficinaId = oficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (bovedaId is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("No existe bóveda abierta para la oficina.", null);
            }

            var cajas = (await connection
                .QueryAsync<CajaChicaCerradaRow>(
                    new CommandDefinition(
                        """
                        SELECT ccd.Id, ccd.SaldoFinal
                        FROM CREDITO.CajaChicaDiario AS ccd WITH (UPDLOCK, HOLDLOCK)
                        INNER JOIN CREDITO.Caja AS c ON c.CajaId = ccd.Id
                        WHERE ccd.IndCierre = CAST(1 AS bit)
                          AND ccd.TransBoveda = CAST(0 AS bit)
                          AND c.OficinaId = @OficinaId;
                        """,
                        new { OficinaId = oficinaId },
                        transaction: transaction,
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false)).ToList();

            if (cajas.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("NO EXISTEN CAJAS POR CERRAR.", null);
            }

            var glosaBase = "CIERRE CAJA CHICA " + fechaOperacion.ToString("d");
            foreach (var caja in cajas)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE CREDITO.CajaChicaDiario
                        SET TransBoveda = CAST(1 AS bit)
                        WHERE Id = @Id
                          AND TransBoveda = CAST(0 AS bit);
                        """,
                        new { caja.Id },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

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
                            'TRE',
                            1,
                            @Glosa,
                            @Importe,
                            CAST(1 AS bit),
                            CAST(1 AS bit),
                            @CajaChicaDiarioId,
                            @UsuarioRegId,
                            @Fecha);
                        """,
                        new
                        {
                            BovedaId = bovedaId.Value,
                            Glosa = glosaBase,
                            Importe = caja.SaldoFinal,
                            CajaChicaDiarioId = caja.Id,
                            UsuarioRegId = usuarioRegId,
                            Fecha = fechaOperacion,
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE b
                    SET
                        Entradas = ISNULL(t.Entradas, 0),
                        Salidas = ISNULL(t.Salidas, 0),
                        SaldoFinal = b.SaldoInicial + ISNULL(t.Entradas, 0) - ISNULL(t.Salidas, 0)
                    FROM CREDITO.Boveda AS b
                    OUTER APPLY (
                        SELECT
                            SUM(CASE WHEN bm.IndEntrada = CAST(1 AS bit) THEN bm.Importe ELSE 0 END) AS Entradas,
                            SUM(CASE WHEN bm.IndEntrada = CAST(0 AS bit) THEN bm.Importe ELSE 0 END) AS Salidas
                        FROM CREDITO.BovedaMov AS bm
                        WHERE bm.BovedaId = b.BovedaId
                          AND bm.Estado = CAST(1 AS bit)
                    ) AS t
                    WHERE b.BovedaId = @BovedaId;
                    """,
                    new { BovedaId = bovedaId.Value },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return (null, new TransferirCierreCajaChicaResponse(cajas.Count));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<(string? Error, CerrarCajaChicaDiarioResponse? Result)> CerrarSesionAsync(
        int usuarioId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId));
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
            var cajaChicaId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.CajaChicaDiario
                    SET IndCierre = CAST(1 AS bit),
                        FechaFinOperacion = @Fecha
                    OUTPUT INSERTED.Id
                    WHERE UsuarioId = @UsuarioId
                      AND IndCierre = CAST(0 AS bit);
                    """,
                    new { UsuarioId = usuarioId, Fecha = fechaOperacion },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (cajaChicaId is null or < 1)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("No existe caja chica abierta para el usuario.", null);
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.Caja
                    SET IndAbierto = CAST(0 AS bit),
                        FechaMod = @Fecha,
                        UsuarioModId = @UsuarioId
                    WHERE CajaId = @CajaChicaId
                      AND Denominacion LIKE '%CAJA CHICA%';
                    """,
                    new { Fecha = fechaOperacion, UsuarioId = usuarioId, CajaChicaId = cajaChicaId.Value },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return (null, new CerrarCajaChicaDiarioResponse(cajaChicaId.Value));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<(string? Error, bool Result)> TransferirSaldosABovedaAsync(
        int oficinaId,
        int usuarioId,
        decimal importe,
        string descripcion,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || usuarioId < 1 || importe <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId));
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            return ("Ingrese descripción.", false);
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
            var boveda = await connection.QueryFirstOrDefaultAsync<BovedaRow>(
                new CommandDefinition(
                    """
                    SELECT TOP 1 BovedaId, IndCierre
                    FROM CREDITO.Boveda
                    WHERE OficinaId = @OficinaId
                    ORDER BY IndTemporal, BovedaId;
                    """,
                    new { OficinaId = oficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (boveda is null || boveda.IndCierre)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("La bóveda no está abierta.", false);
            }

            var cajaChica = await connection.QueryFirstOrDefaultAsync<CajaChicaAbiertaRow>(
                new CommandDefinition(
                    """
                    SELECT ccd.Id, ccd.SaldoFinal
                    FROM CREDITO.CajaChicaDiario AS ccd WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN CREDITO.Caja AS c ON c.CajaId = ccd.Id
                    WHERE ccd.UsuarioId = @UsuarioId
                      AND ccd.IndCierre = CAST(0 AS bit)
                      AND c.OficinaId = @OficinaId;
                    """,
                    new { UsuarioId = usuarioId, OficinaId = oficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (cajaChica is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("No existe caja chica abierta.", false);
            }

            if (importe > cajaChica.SaldoFinal)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("Saldo Insuficiente!", false);
            }

            var personaOficinaId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT TOP 1 u.PersonaId
                    FROM MAESTRO.Oficina AS o
                    INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = o.UsuarioAsignadoId
                    WHERE o.OficinaId = @OficinaId;
                    """,
                    new { OficinaId = oficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (personaOficinaId is null or < 1)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("No se pudo resolver persona de la oficina.", false);
            }

            var descBoveda = "TRANS DE CAJA CHICA: " + descripcion.Trim();
            var descCaja = "TRANS A BOVEDA: " + descripcion.Trim();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO CREDITO.MovimientoCajaChica (
                        CajaChicaDiarioId,
                        PersonaId,
                        Operacion,
                        Importe,
                        Descripcion,
                        IndEntrada,
                        Estado,
                        UsuarioRegId,
                        FechaReg,
                        IndRendido,
                        ImporteRendido)
                    VALUES (
                        @CajaChicaDiarioId,
                        @PersonaId,
                        'TRS',
                        @Importe,
                        @Descripcion,
                        CAST(0 AS bit),
                        CAST(1 AS bit),
                        @UsuarioRegId,
                        @FechaReg,
                        CAST(0 AS bit),
                        0);
                    """,
                    new
                    {
                        CajaChicaDiarioId = cajaChica.Id,
                        PersonaId = personaOficinaId.Value,
                        Importe = importe,
                        Descripcion = descCaja,
                        UsuarioRegId = usuarioId,
                        FechaReg = fechaOperacion,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

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
                        'TRE',
                        1,
                        @Glosa,
                        @Importe,
                        CAST(1 AS bit),
                        CAST(1 AS bit),
                        @CajaChicaDiarioId,
                        @UsuarioRegId,
                        @FechaReg);
                    """,
                    new
                    {
                        BovedaId = boveda.BovedaId,
                        Glosa = descBoveda,
                        Importe = importe,
                        CajaChicaDiarioId = cajaChica.Id,
                        UsuarioRegId = usuarioId,
                        FechaReg = fechaOperacion,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE ccd
                    SET
                        Entradas = ISNULL(t.Entradas, 0),
                        Salidas = ISNULL(t.Salidas, 0),
                        SaldoFinal = ccd.SaldoInicial + ISNULL(t.Entradas, 0) - ISNULL(t.Salidas, 0)
                    FROM CREDITO.CajaChicaDiario AS ccd
                    OUTER APPLY (
                        SELECT
                            SUM(CASE WHEN mc.IndEntrada = CAST(1 AS bit) THEN mc.Importe ELSE 0 END) AS Entradas,
                            SUM(CASE WHEN mc.IndEntrada = CAST(0 AS bit) THEN mc.Importe ELSE 0 END) AS Salidas
                        FROM CREDITO.MovimientoCajaChica AS mc
                        WHERE mc.CajaChicaDiarioId = ccd.Id
                          AND mc.Estado = CAST(1 AS bit)
                    ) AS t
                    WHERE ccd.Id = @CajaChicaDiarioId;
                    """,
                    new { CajaChicaDiarioId = cajaChica.Id },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId;",
                    new { BovedaId = boveda.BovedaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return (null, true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private sealed class CajaChicaCerradaRow
    {
        public int Id { get; init; }
        public decimal SaldoFinal { get; init; }
    }

    private sealed class BovedaRow
    {
        public int BovedaId { get; init; }
        public bool IndCierre { get; init; }
    }

    private sealed class CajaChicaAbiertaRow
    {
        public int Id { get; init; }
        public decimal SaldoFinal { get; init; }
    }
}
