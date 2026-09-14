using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class MovimientoCajaScopeReadService(IOptions<SqlDatabaseOptions> options)
    : IMovimientoCajaScopeReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<MovimientoCajaScopeDto?> GetScopeAsync(
        int movimientoCajaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (movimientoCajaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoCajaId), "movimientoCajaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                c.OficinaId,
                mc.CajaDiarioId,
                mc.Operacion,
                mc.Estado AS EstadoActivo,
                cd.IndCierre AS CajaDiarioCerrada
            FROM CREDITO.MovimientoCaja AS mc
            INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = mc.CajaDiarioId
            INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
            WHERE mc.MovimientoCajaId = @MovimientoCajaId;
            """;
        var command = new CommandDefinition(
            sql,
            new { MovimientoCajaId = movimientoCajaId },
            cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<MovimientoCajaScopeDto>(command).ConfigureAwait(false);
    }

    public async Task<bool> EsBloqueoAnularPorPagosCuotaAsync(
        int movimientoCajaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (movimientoCajaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoCajaId), "movimientoCajaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM CREDITO.MovimientoCaja AS mc
                INNER JOIN CREDITO.CuentaxCobrar AS cx ON cx.MovimientoCajaId = mc.MovimientoCajaId
                INNER JOIN CREDITO.PlanPago AS pp ON pp.CreditoId = cx.CreditoId
                WHERE mc.MovimientoCajaId = @MovimientoCajaId
                  AND mc.Operacion = 'INI'
                  AND cx.CreditoId IS NOT NULL
                  AND pp.Estado = 'PAG'
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;
        var command = new CommandDefinition(
            sql,
            new { MovimientoCajaId = movimientoCajaId },
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command).ConfigureAwait(false);
    }

    public async Task<MovimientoCajaAnularPreviewDto?> GetAnularPreviewAsync(
        int movimientoCajaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (movimientoCajaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoCajaId), "movimientoCajaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                mc.MovimientoCajaId,
                mc.CajaDiarioId,
                RTRIM(mc.Operacion) AS Operacion,
                mc.Descripcion,
                p.NombreCompleto AS Persona,
                mc.FechaReg,
                mc.ImportePago,
                mc.Estado AS EstadoActivo,
                cd.IndCierre AS CajaDiarioCerrada,
                c.OficinaId
            FROM CREDITO.MovimientoCaja AS mc
            INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = mc.CajaDiarioId
            INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
            LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = mc.PersonaId
            WHERE mc.MovimientoCajaId = @MovimientoCajaId;
            """;
        var command = new CommandDefinition(
            sql,
            new { MovimientoCajaId = movimientoCajaId },
            cancellationToken: cancellationToken);
        var row = await connection.QueryFirstOrDefaultAsync<AnularPreviewRow>(command).ConfigureAwait(false);
        if (row is null)
            return null;

        string? bloqueo = null;
        if (!row.EstadoActivo)
            bloqueo = "El movimiento se encuentra anulado.";
        else if (row.CajaDiarioCerrada)
            bloqueo = "Caja diario cerrado; no se puede anular.";
        else if (await EsBloqueoAnularPorPagosCuotaAsync(movimientoCajaId, cancellationToken)
                     .ConfigureAwait(false))
            bloqueo = "Tiene Pagos de cuotas, No se Puede Anular el Crédito";

        return new MovimientoCajaAnularPreviewDto(
            row.MovimientoCajaId,
            row.CajaDiarioId,
            row.OficinaId,
            row.Operacion,
            row.Descripcion,
            row.Persona,
            row.FechaReg,
            row.ImportePago,
            bloqueo is null,
            bloqueo);
    }

    private sealed class AnularPreviewRow
    {
        public int MovimientoCajaId { get; init; }
        public int CajaDiarioId { get; init; }
        public string Operacion { get; init; } = string.Empty;
        public string? Descripcion { get; init; }
        public string? Persona { get; init; }
        public DateTime FechaReg { get; init; }
        public decimal ImportePago { get; init; }
        public bool EstadoActivo { get; init; }
        public bool CajaDiarioCerrada { get; init; }
        public int OficinaId { get; init; }
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
