using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Prendario;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class DesembolsoWriteService(IOptions<SqlDatabaseOptions> options) : IDesembolsoWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<RealizarDesembolsoResponse> RealizarAsync(
        int cajaDiarioId,
        int creditoId,
        int usuarioId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default)
    {
        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
        }

        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId debe ser >= 1.");
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
            var existente = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT MovimientoCajaId
                    FROM CREDITO.MovimientoCaja
                    WHERE CreditoId = @CreditoId
                      AND Operacion = 'DES'
                      AND Estado = CAST(1 AS bit);
                    """,
                    new { CreditoId = creditoId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (existente is > 0)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new RealizarDesembolsoResponse(existente.Value, YaRegistrado: true);
            }

            var credito = await connection.QueryFirstOrDefaultAsync<CreditoDesembolsoRow>(
                new CommandDefinition(
                    """
                    SELECT CreditoId, PersonaId, MontoDesembolso, Estado
                    FROM CREDITO.Credito
                    WHERE CreditoId = @CreditoId;
                    """,
                    new { CreditoId = creditoId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (credito is null)
            {
                throw new KeyNotFoundException($"No existe crédito {creditoId}.");
            }

            if (!string.Equals(credito.Estado, "APR", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"El crédito {creditoId} no está en estado APR (actual: {credito.Estado}).");
            }

            var cajaCerrada = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT IndCierre
                    FROM CREDITO.CajaDiario
                    WHERE CajaDiarioId = @CajaDiarioId;
                    """,
                    new { CajaDiarioId = cajaDiarioId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (cajaCerrada)
            {
                throw new InvalidOperationException("La caja diario está cerrada.");
            }

            var filasCredito = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.Credito
                    SET FechaDesembolso = @Fecha,
                        Estado = 'DES',
                        UsuarioModId = @UsuarioId,
                        FechaMod = @Fecha
                    WHERE CreditoId = @CreditoId
                      AND Estado = 'APR';
                    """,
                    new
                    {
                        CreditoId = creditoId,
                        Fecha = fechaOperacion,
                        UsuarioId = usuarioId,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (filasCredito == 0)
            {
                throw new InvalidOperationException("No se pudo actualizar el crédito a DES (estado distinto de APR).");
            }

            // Prendario: el reloj del contrato parte del desembolso real (paridad Anexo A/B).
            await RealinearFechasPrendarioSiAplicaAsync(
                    connection,
                    transaction,
                    creditoId,
                    fechaOperacion.Date,
                    cancellationToken)
                .ConfigureAwait(false);

            var descripcion = $"DESEMBOLSO CREDITO {creditoId}";
            var movimientoCajaId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO CREDITO.MovimientoCaja (
                        CajaDiarioId, Operacion, ImportePago, PersonaId, Descripcion,
                        IndEntrada, Estado, TipoPagoId, UsuarioRegId, FechaReg, OrdenVentaId, CreditoId)
                    VALUES (
                        @CajaDiarioId, 'DES', @ImportePago, @PersonaId, @Descripcion,
                        CAST(0 AS bit), CAST(1 AS bit), 1, @UsuarioRegId, @FechaReg, NULL, @CreditoId);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new
                    {
                        CajaDiarioId = cajaDiarioId,
                        ImportePago = credito.MontoDesembolso,
                        PersonaId = credito.PersonaId,
                        Descripcion = descripcion,
                        UsuarioRegId = usuarioId,
                        FechaReg = fechaOperacion,
                        CreditoId = creditoId,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

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

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new RealizarDesembolsoResponse(movimientoCajaId, YaRegistrado: false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Realinea plan, 1.er pago, vencimiento y remate desde la fecha de desembolso.
    /// Cuota n = desembolso + n periodos (M=mes, Q=15d, S=7d, D=1d). Remate = vencimiento + 30.
    /// </summary>
    private static async Task RealinearFechasPrendarioSiAplicaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int creditoId,
        DateTime fechaDesembolso,
        CancellationToken cancellationToken)
    {
        var meta = await connection.QueryFirstOrDefaultAsync<PrendarioDesembolsoMetaRow>(
            new CommandDefinition(
                """
                SELECT EsPrendario, FormaPago, NumeroCuotas
                FROM CREDITO.Credito
                WHERE CreditoId = @CreditoId;
                """,
                new { CreditoId = creditoId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (meta is null || !meta.EsPrendario || meta.NumeroCuotas < 1)
        {
            return;
        }

        var forma = string.IsNullOrWhiteSpace(meta.FormaPago)
            ? "M"
            : char.ToUpperInvariant(meta.FormaPago.Trim()[0]).ToString();

        var numeros = (await connection.QueryAsync<int>(
            new CommandDefinition(
                """
                SELECT Numero
                FROM CREDITO.PlanPago
                WHERE CreditoId = @CreditoId
                ORDER BY Numero;
                """,
                new { CreditoId = creditoId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).AsList();

        if (numeros.Count == 0)
        {
            // Sin plan aún: solo cabecera según N cuotas.
            var primer = PrendarioFechas.AvanzarPeriodo(fechaDesembolso, forma, 1);
            var venc = PrendarioFechas.AvanzarPeriodo(fechaDesembolso, forma, meta.NumeroCuotas);
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.Credito
                    SET FechaPrimerPago = @Primer,
                        FechaVencimiento = @Venc,
                        FechaRemate = DATEADD(DAY, 30, @Venc)
                    WHERE CreditoId = @CreditoId;
                    """,
                    new { CreditoId = creditoId, Primer = primer, Venc = venc },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            return;
        }

        DateTime? primerPago = null;
        DateTime? ultimoVenc = null;
        foreach (var numero in numeros)
        {
            var vencCuota = PrendarioFechas.AvanzarPeriodo(fechaDesembolso, forma, numero);
            primerPago ??= vencCuota;
            ultimoVenc = vencCuota;
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.PlanPago
                    SET FechaVencimiento = @Fecha
                    WHERE CreditoId = @CreditoId
                      AND Numero = @Numero;
                    """,
                    new { CreditoId = creditoId, Numero = numero, Fecha = vencCuota },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        if (primerPago is null || ultimoVenc is null)
        {
            return;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE CREDITO.Credito
                SET FechaPrimerPago = @Primer,
                    FechaVencimiento = @Venc,
                    FechaRemate = DATEADD(DAY, 30, @Venc)
                WHERE CreditoId = @CreditoId;
                """,
                new { CreditoId = creditoId, Primer = primerPago.Value, Venc = ultimoVenc.Value },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private sealed class CreditoDesembolsoRow
    {
        public int CreditoId { get; init; }
        public int PersonaId { get; init; }
        public decimal MontoDesembolso { get; init; }
        public string Estado { get; init; } = string.Empty;
    }

    private sealed class PrendarioDesembolsoMetaRow
    {
        public bool EsPrendario { get; init; }
        public string? FormaPago { get; init; }
        public int NumeroCuotas { get; init; }
    }
}
