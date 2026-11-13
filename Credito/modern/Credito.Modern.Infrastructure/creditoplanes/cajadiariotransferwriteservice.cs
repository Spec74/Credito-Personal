using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CajaDiarioTransferWriteService(IOptions<SqlDatabaseOptions> options)
    : ICajaDiarioTransferWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<(string? Error, bool Success)> TransferirSaldosAsync(
        int oficinaId,
        int cajaDiarioOrigenId,
        int usuarioRegId,
        decimal importe,
        string descripcion,
        int? cajaIdDestino,
        DateTime fechaOperacion,
        short tipoPagoId = 1,
        CancellationToken cancellationToken = default)
    {
        importe = decimal.Round(importe, 2, MidpointRounding.AwayFromZero);
        if (oficinaId < 1 || cajaDiarioOrigenId < 1 || usuarioRegId < 1 || importe <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId));
        }

        if (tipoPagoId < 1 || tipoPagoId is >= 6 and <= 8)
        {
            return ("Tipo de pago inválido.", false);
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
            var origen = await connection.QueryFirstOrDefaultAsync<CajaDiarioRow>(
                new CommandDefinition(
                    """
                    SELECT cd.CajaDiarioId, cd.CajaId, cd.IndCierre, c.Denominacion, cd.SaldoFinal
                    FROM CREDITO.CajaDiario AS cd WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                    WHERE cd.CajaDiarioId = @CajaDiarioId
                      AND c.OficinaId = @OficinaId;
                    """,
                    new { CajaDiarioId = cajaDiarioOrigenId, OficinaId = oficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (origen is null || origen.IndCierre)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("La caja diario de origen no está abierta.", false);
            }

            var esCentral = await TipoPagoOficinaSql
                .EsCentralPorOficinaAsync(connection, oficinaId, cancellationToken, transaction)
                .ConfigureAwait(false);
            var tipoErr = TipoPagoRules.RequireCobroPermitido(tipoPagoId, esCentral, "tipoPagoId");
            if (tipoErr is not null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return (tipoErr, false);
            }

            var saldoMedio = await ObtenerSaldoMedioCajaDiarioAsync(
                connection,
                transaction,
                origen.CajaDiarioId,
                tipoPagoId,
                cancellationToken).ConfigureAwait(false);
            if (saldoMedio < importe)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return (
                    $"El monto a transferir debe ser menor o igual al saldo del medio (disponible {saldoMedio:0.00}).",
                    false);
            }

            var desc = descripcion.Trim();

            if (cajaIdDestino is > 0)
            {
                var error = await TransferirACajaAsync(
                    connection,
                    transaction,
                    origen,
                    cajaIdDestino.Value,
                    oficinaId,
                    usuarioRegId,
                    importe,
                    desc,
                    fechaOperacion,
                    tipoPagoId,
                    cancellationToken).ConfigureAwait(false);
                if (error is not null)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return (error, false);
                }
            }
            else
            {
                var error = await TransferirABovedaAsync(
                    connection,
                    transaction,
                    origen.CajaDiarioId,
                    oficinaId,
                    usuarioRegId,
                    importe,
                    desc,
                    fechaOperacion,
                    tipoPagoId,
                    cancellationToken).ConfigureAwait(false);
                if (error is not null)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return (error, false);
                }
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return (null, true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<string?> TransferirACajaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CajaDiarioRow origen,
        int cajaIdDestino,
        int oficinaId,
        int usuarioRegId,
        decimal importe,
        string descripcion,
        DateTime fechaOperacion,
        short tipoPagoId,
        CancellationToken cancellationToken)
    {
        var destino = await connection.QueryFirstOrDefaultAsync<CajaDiarioDestinoRow>(
            new CommandDefinition(
                """
                SELECT cd.CajaDiarioId, cd.IndCierre, c.Denominacion
                FROM CREDITO.CajaDiario AS cd WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                WHERE cd.CajaId = @CajaId
                  AND c.OficinaId = @OficinaId
                  AND cd.IndCierre = CAST(0 AS bit);
                """,
                new { CajaId = cajaIdDestino, OficinaId = oficinaId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (destino is null)
        {
            return "Seleccione una caja abierta destino.";
        }

        if (destino.CajaDiarioId == origen.CajaDiarioId)
        {
            return "La caja destino debe ser distinta a la caja actual.";
        }

        var personaOrigenId = await ResolverPersonaCajaDiarioAsync(
            connection,
            transaction,
            origen.CajaDiarioId,
            cancellationToken).ConfigureAwait(false);
        var personaDestinoId = await ResolverPersonaCajaDiarioAsync(
            connection,
            transaction,
            destino.CajaDiarioId,
            cancellationToken).ConfigureAwait(false);

        if (personaOrigenId is null or < 1 || personaDestinoId is null or < 1)
        {
            return "No se pudo resolver la persona del cajero.";
        }

        var descOrigen =
            $"TRANS A CAJA {destino.Denominacion}: {descripcion}";

        var movOrigenId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                INSERT INTO CREDITO.MovimientoCaja (
                    CajaDiarioId,
                    Operacion,
                    ImportePago,
                    Descripcion,
                    IndEntrada,
                    Estado,
                    PersonaId,
                    TipoPagoId,
                    UsuarioRegId,
                    FechaReg)
                VALUES (
                    @CajaDiarioId,
                    'TRS',
                    @ImportePago,
                    @Descripcion,
                    CAST(0 AS bit),
                    CAST(1 AS bit),
                    @PersonaId,
                    @TipoPagoId,
                    @UsuarioRegId,
                    @FechaReg);
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """,
                new
                {
                    CajaDiarioId = origen.CajaDiarioId,
                    ImportePago = importe,
                    Descripcion = descOrigen,
                    PersonaId = personaOrigenId.Value,
                    TipoPagoId = tipoPagoId,
                    UsuarioRegId = usuarioRegId,
                    FechaReg = fechaOperacion,
                },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var descDestino =
            $"[MovCajero:{movOrigenId}] TRANS DE CAJA {origen.Denominacion}: {descripcion}";

        var movDestinoId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                INSERT INTO CREDITO.MovimientoCaja (
                    CajaDiarioId,
                    Operacion,
                    ImportePago,
                    Descripcion,
                    IndEntrada,
                    Estado,
                    PersonaId,
                    TipoPagoId,
                    UsuarioRegId,
                    FechaReg)
                VALUES (
                    @CajaDiarioId,
                    'TRE',
                    @ImportePago,
                    @Descripcion,
                    CAST(1 AS bit),
                    CAST(1 AS bit),
                    @PersonaId,
                    @TipoPagoId,
                    @UsuarioRegId,
                    @FechaReg);
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """,
                new
                {
                    CajaDiarioId = destino.CajaDiarioId,
                    ImportePago = importe,
                    Descripcion = descDestino,
                    PersonaId = personaDestinoId.Value,
                    TipoPagoId = tipoPagoId,
                    UsuarioRegId = usuarioRegId,
                    FechaReg = fechaOperacion,
                },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE CREDITO.MovimientoCaja
                SET Descripcion = @Descripcion
                WHERE MovimientoCajaId = @MovimientoCajaId;
                """,
                new
                {
                    MovimientoCajaId = movOrigenId,
                    Descripcion = $"[MovCajero:{movDestinoId}] {descOrigen}",
                },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        await ActualizarSaldosCajaDiarioAsync(
            connection,
            transaction,
            origen.CajaDiarioId,
            cancellationToken).ConfigureAwait(false);
        await ActualizarSaldosCajaDiarioAsync(
            connection,
            transaction,
            destino.CajaDiarioId,
            cancellationToken).ConfigureAwait(false);

        return null;
    }

    private static async Task<string?> TransferirABovedaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int cajaDiarioOrigenId,
        int oficinaId,
        int usuarioRegId,
        decimal importe,
        string descripcion,
        DateTime fechaOperacion,
        short tipoPagoId,
        CancellationToken cancellationToken)
    {
        importe = Math.Round(importe, 2, MidpointRounding.AwayFromZero);
        if (importe <= 0)
        {
            return "El importe debe ser mayor a 0.";
        }

        // Misma regla que usp_CerrarCajasDiarios: con temporal abierta, el retorno va a temporal.
        var boveda = await connection.QueryFirstOrDefaultAsync<BovedaRow>(
            new CommandDefinition(
                BovedaOperativaSql.SelectRowDestinoRetornoCajas,
                new { OficinaId = oficinaId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (boveda is null || boveda.IndCierre)
        {
            return "La bóveda no está abierta.";
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
            return "No se pudo resolver persona de la oficina.";
        }

        var glosaBoveda = "TRANS DE CAJA: " + descripcion;

        var movimientoBovedaId = await connection.ExecuteScalarAsync<int>(
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
                OUTPUT INSERTED.MovimientoBovedaId
                VALUES (
                    @BovedaId,
                    'TRE',
                    @TipoPagoId,
                    @Glosa,
                    @Importe,
                    CAST(1 AS bit),
                    CAST(1 AS bit),
                    @CajaDiarioId,
                    @UsuarioRegId,
                    @FechaReg);
                """,
                new
                {
                    BovedaId = boveda.BovedaId,
                    TipoPagoId = tipoPagoId,
                    Glosa = glosaBoveda,
                    Importe = importe,
                    CajaDiarioId = cajaDiarioOrigenId,
                    UsuarioRegId = usuarioRegId,
                    FechaReg = fechaOperacion,
                },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO CREDITO.MovimientoCaja (
                    CajaDiarioId,
                    Operacion,
                    ImportePago,
                    Descripcion,
                    IndEntrada,
                    Estado,
                    PersonaId,
                    TipoPagoId,
                    UsuarioRegId,
                    FechaReg)
                VALUES (
                    @CajaDiarioId,
                    'TRS',
                    @ImportePago,
                    @Descripcion,
                    CAST(0 AS bit),
                    CAST(1 AS bit),
                    @PersonaId,
                    @TipoPagoId,
                    @UsuarioRegId,
                    @FechaReg);
                """,
                new
                {
                    CajaDiarioId = cajaDiarioOrigenId,
                    ImportePago = importe,
                    Descripcion =
                        $"[MovBoveda:{movimientoBovedaId}] TRANS A BOVEDA: {descripcion}",
                    PersonaId = personaOficinaId.Value,
                    TipoPagoId = tipoPagoId,
                    UsuarioRegId = usuarioRegId,
                    FechaReg = fechaOperacion,
                },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        await ActualizarSaldosCajaDiarioAsync(
            connection,
            transaction,
            cajaDiarioOrigenId,
            cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId;",
                new { BovedaId = boveda.BovedaId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return null;
    }

    private static async Task<int?> ResolverPersonaCajaDiarioAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int cajaDiarioId,
        CancellationToken cancellationToken)
    {
        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                """
                SELECT TOP 1 u.PersonaId
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = cd.UsuarioAsignadoId
                WHERE cd.CajaDiarioId = @CajaDiarioId;
                """,
                new { CajaDiarioId = cajaDiarioId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>
    /// Disponible del medio en caja: movimientos del TipoPagoId (+ SaldoInicial solo si es efectivo).
    /// </summary>
    private static async Task<decimal> ObtenerSaldoMedioCajaDiarioAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int cajaDiarioId,
        short tipoPagoId,
        CancellationToken cancellationToken)
    {
        return await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(
                """
                SELECT
                    IIF(@TipoPagoId = 1, cd.SaldoInicial, CAST(0 AS decimal(16, 2)))
                    + ISNULL(SUM(CASE
                        WHEN mc.TipoPagoId = @TipoPagoId AND mc.IndEntrada = CAST(1 AS bit)
                            THEN mc.ImportePago
                        WHEN mc.TipoPagoId = @TipoPagoId AND mc.IndEntrada = CAST(0 AS bit)
                            THEN -mc.ImportePago
                        ELSE 0 END), 0)
                FROM CREDITO.CajaDiario AS cd WITH (UPDLOCK, HOLDLOCK)
                LEFT JOIN CREDITO.MovimientoCaja AS mc WITH (UPDLOCK, HOLDLOCK)
                    ON mc.CajaDiarioId = cd.CajaDiarioId
                   AND mc.Estado = CAST(1 AS bit)
                WHERE cd.CajaDiarioId = @CajaDiarioId
                GROUP BY cd.CajaDiarioId, cd.SaldoInicial;
                """,
                new { CajaDiarioId = cajaDiarioId, TipoPagoId = tipoPagoId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static async Task ActualizarSaldosCajaDiarioAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int cajaDiarioId,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE cd
                SET
                    Entradas = ISNULL(agg.Entradas, 0),
                    Salidas = ISNULL(agg.Salidas, 0),
                    SaldoFinal = cd.SaldoInicial + ISNULL(agg.Entradas, 0) - ISNULL(agg.Salidas, 0)
                FROM CREDITO.CajaDiario AS cd
                OUTER APPLY (
                    SELECT
                        SUM(CASE WHEN mc.IndEntrada = CAST(1 AS bit) THEN mc.ImportePago ELSE 0 END) AS Entradas,
                        SUM(CASE WHEN mc.IndEntrada = CAST(0 AS bit) THEN mc.ImportePago ELSE 0 END) AS Salidas
                    FROM CREDITO.MovimientoCaja AS mc
                    WHERE mc.CajaDiarioId = cd.CajaDiarioId
                      AND mc.Estado = CAST(1 AS bit)
                ) AS agg
                WHERE cd.CajaDiarioId = @CajaDiarioId;
                """,
                new { CajaDiarioId = cajaDiarioId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private sealed class CajaDiarioRow
    {
        public int CajaDiarioId { get; init; }
        public int CajaId { get; init; }
        public bool IndCierre { get; init; }
        public decimal SaldoFinal { get; init; }
        public string Denominacion { get; init; } = string.Empty;
    }

    private sealed class CajaDiarioDestinoRow
    {
        public int CajaDiarioId { get; init; }
        public bool IndCierre { get; init; }
        public string Denominacion { get; init; } = string.Empty;
    }

    private sealed class BovedaRow
    {
        public int BovedaId { get; init; }
        public bool IndCierre { get; init; }
    }
}
