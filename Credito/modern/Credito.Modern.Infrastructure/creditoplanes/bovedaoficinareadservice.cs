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
}
