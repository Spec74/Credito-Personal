using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class BovedaOficinaReadService(IOptions<SqlDatabaseOptions> options) : IBovedaOficinaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<int?> GetOficinaIdByBovedaIdAsync(int bovedaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (bovedaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bovedaId), "bovedaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = "SELECT OficinaId FROM CREDITO.Boveda WHERE BovedaId = @BovedaId;";
        var command = new CommandDefinition(
            sql,
            new { BovedaId = bovedaId },
            cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<int?>(command).ConfigureAwait(false);
    }

    public async Task<BovedaAbiertaDto?> GetCabeceraReporteAsync(
        int bovedaId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (bovedaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bovedaId), "bovedaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT BovedaId,
                   OficinaId,
                   SaldoInicial,
                   Entradas,
                   Salidas,
                   SaldoFinal,
                   FechaIniOperacion,
                   FechaFinOperacion,
                   IndCierre,
                   IndTemporal
            FROM CREDITO.Boveda
            WHERE BovedaId = @BovedaId;
            """;
        var row = await connection
            .QueryFirstOrDefaultAsync<BovedaCabRow>(
                new CommandDefinition(sql, new { BovedaId = bovedaId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return row is null
            ? null
            : new BovedaAbiertaDto(
                row.BovedaId,
                row.OficinaId,
                row.SaldoInicial,
                row.Entradas,
                row.Salidas,
                row.SaldoFinal,
                row.FechaIniOperacion,
                row.FechaFinOperacion,
                row.IndCierre,
                row.IndTemporal);
    }

    private sealed class BovedaCabRow
    {
        public int BovedaId { get; init; }
        public int OficinaId { get; init; }
        public decimal SaldoInicial { get; init; }
        public decimal Entradas { get; init; }
        public decimal Salidas { get; init; }
        public decimal SaldoFinal { get; init; }
        public DateTime FechaIniOperacion { get; init; }
        public DateTime? FechaFinOperacion { get; init; }
        public bool IndCierre { get; init; }
        public bool IndTemporal { get; init; }
    }

    public async Task<bool> ExisteBovedaTemporalAbiertaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 FROM CREDITO.Boveda
                WHERE OficinaId = @OficinaId AND IndCierre = 0 AND IndTemporal = 1
            ) THEN 1 ELSE 0 END AS bit);
            """;
        var command = new CommandDefinition(
            sql,
            new { OficinaId = oficinaId },
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BovedaDestinoTransferenciaDto>> ListarDestinosTransferenciaAsync(
        int oficinaOrigenId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (oficinaOrigenId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaOrigenId), "oficinaOrigenId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                b.BovedaId,
                b.OficinaId,
                COALESCE(o.Denominacion, CONCAT('Oficina ', b.OficinaId)) AS Oficina,
                b.SaldoFinal,
                b.FechaIniOperacion
            FROM CREDITO.Boveda AS b
            LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = b.OficinaId
            WHERE b.IndCierre = CAST(0 AS bit)
              AND b.IndTemporal = CAST(0 AS bit)
              AND b.OficinaId <> @OficinaOrigenId
            ORDER BY o.Denominacion, b.BovedaId;
            """;
        var rows = await connection
            .QueryAsync<BovedaDestinoTransferenciaDto>(
                new CommandDefinition(
                    sql,
                    new { OficinaOrigenId = oficinaOrigenId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.ToList();
    }
}
