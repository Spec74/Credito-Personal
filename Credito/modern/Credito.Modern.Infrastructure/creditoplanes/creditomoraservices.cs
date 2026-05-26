using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CreditoMoraReadService(IOptions<SqlDatabaseOptions> options) : ICreditoMoraReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<CreditoMoraRowDto>> ListarPorCreditoAsync(
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                cm.CreditoMoraId,
                cm.CreditoId,
                cm.MovimientoCajaId,
                cm.Fecha,
                cm.Mora,
                cm.DiasAtrazo,
                cm.SaldoMora,
                cm.InteresMora
            FROM CREDITO.CreditoMora AS cm
            WHERE cm.CreditoId = @CreditoId
            ORDER BY cm.CreditoMoraId DESC;
            """;
        var rows = await connection
            .QueryAsync<CreditoMoraRowDto>(
                new CommandDefinition(sql, new { CreditoId = creditoId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<decimal> ObtenerSaldoPostergadoAsync(
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(
                """
                SELECT ISNULL(SUM(SaldoMora), 0)
                FROM CREDITO.CreditoMora
                WHERE CreditoId = @CreditoId
                  AND MovimientoCajaId IS NULL;
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> CreditoTieneMoraPostergadaHabilitadaAsync(
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var indMora = await connection.ExecuteScalarAsync<bool?>(
            new CommandDefinition(
                """
                SELECT p.IndMora
                FROM CREDITO.Credito AS c
                INNER JOIN CREDITO.Producto AS p ON p.ProductoId = c.ProductoId
                WHERE c.CreditoId = @CreditoId;
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return indMora == true;
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}

public sealed class CreditoMoraWriteService(IOptions<SqlDatabaseOptions> options) : ICreditoMoraWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task RegistrarPostergadaAsync(
        int creditoId,
        DateTime fechaVencimientoCuota,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(
                "CREDITO.usp_CreditoMora_Registrar",
                new { CreditoId = creditoId, FechaVencimiento = fechaVencimientoCuota.Date },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task LiquidarAcumuladasAsync(
        int creditoId,
        int cajaDiarioId,
        int usuarioId,
        int tipoPagoId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(
                "CREDITO.usp_CreditoMora_Liquidar",
                new
                {
                    CreditoId = creditoId,
                    CajaDiarioId = cajaDiarioId,
                    UsuarioId = usuarioId,
                    TipoPagoId = tipoPagoId,
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}

public sealed class CajaPagoMoraOrchestrator(
    ICreditoMoraReadService moraRead,
    ICreditoMoraWriteService moraWrite,
    IOptions<SqlDatabaseOptions> options) : ICajaPagoMoraOrchestrator
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task AplicarTrasPagoCuotasAsync(
        int creditoId,
        int cajaDiarioId,
        int usuarioId,
        int tipoPagoId,
        IReadOnlyList<int> planPagoIdsPagados,
        bool esUltimaCuota,
        CancellationToken cancellationToken = default)
    {
        if (planPagoIdsPagados.Count == 0)
        {
            return;
        }

        if (!await moraRead.CreditoTieneMoraPostergadaHabilitadaAsync(creditoId, cancellationToken)
                .ConfigureAwait(false))
        {
            return;
        }

        var pagadas = await ObtenerCuotasParaMoraAsync(creditoId, planPagoIdsPagados, cancellationToken)
            .ConfigureAwait(false);

        foreach (var cuota in pagadas)
        {
            var tieneMora = (cuota.ImporteMora ?? 0) > 0
                || (cuota.FechaVencimiento.HasValue
                    && cuota.FechaVencimiento.Value.Date < DateTime.Today);
            if (!tieneMora || !cuota.FechaVencimiento.HasValue)
            {
                continue;
            }

            await moraWrite
                .RegistrarPostergadaAsync(creditoId, cuota.FechaVencimiento.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        if (esUltimaCuota)
        {
            await moraWrite
                .LiquidarAcumuladasAsync(creditoId, cajaDiarioId, usuarioId, tipoPagoId, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task<PagoCajaResultResponse> ProcesarPagoCuotaConMoraAsync(
        int cajaDiarioId,
        int creditoId,
        decimal importeRecibido,
        int usuarioId,
        int tipoPagoId,
        string fechaPagoTransferencia,
        bool esUltimaCuota,
        CancellationToken cancellationToken = default)
    {
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
            var movimientoCuotaId = await connection.QueryFirstOrDefaultAsync<int?>(
                new CommandDefinition(
                    "CREDITO.usp_PagarCuotaPagoLibre",
                    new
                    {
                        CajaDiarioId = cajaDiarioId,
                        CreditoId = creditoId,
                        ImporteRecibido = importeRecibido,
                        UsuarioId = usuarioId,
                        TipoPagoId = tipoPagoId,
                        FechaPagoTransferencia = fechaPagoTransferencia ?? string.Empty,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (movimientoCuotaId is > 0 && esUltimaCuota
                && await moraRead.CreditoTieneMoraPostergadaHabilitadaAsync(creditoId, cancellationToken)
                    .ConfigureAwait(false))
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "CREDITO.usp_CreditoMora_Liquidar",
                        new
                        {
                            CreditoId = creditoId,
                            CajaDiarioId = cajaDiarioId,
                            UsuarioId = usuarioId,
                            TipoPagoId = tipoPagoId,
                        },
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new PagoCajaResultResponse(movimientoCuotaId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<IReadOnlyList<CuotaMoraSnapshot>> ObtenerCuotasParaMoraAsync(
        int creditoId,
        IReadOnlyList<int> planPagoIds,
        CancellationToken cancellationToken)
    {
        if (planPagoIds.Count == 0)
        {
            return [];
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                pp.PlanPagoId,
                pp.FechaVencimiento,
                pp.ImporteMora
            FROM CREDITO.PlanPago AS pp
            WHERE pp.CreditoId = @CreditoId
              AND pp.PlanPagoId IN @PlanPagoIds;
            """;
        var rows = await connection
            .QueryAsync<CuotaMoraSnapshot>(
                new CommandDefinition(
                    sql,
                    new { CreditoId = creditoId, PlanPagoIds = planPagoIds },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.AsList();
    }

    private sealed class CuotaMoraSnapshot
    {
        public int PlanPagoId { get; init; }
        public DateTime? FechaVencimiento { get; init; }
        public decimal? ImporteMora { get; init; }
    }
}
