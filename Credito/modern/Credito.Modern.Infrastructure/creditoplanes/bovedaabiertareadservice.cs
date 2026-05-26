using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class BovedaAbiertaReadService(IOptions<SqlDatabaseOptions> options)
    : IBovedaAbiertaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<BovedaAbiertaDto?> ObtenerAbiertaPorOficinaAsync(
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
            SELECT TOP 1
                BovedaId,
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
            WHERE OficinaId = @OficinaId
              AND IndCierre = CAST(0 AS bit)
            ORDER BY IndTemporal, BovedaId;
            """;

        return await connection
            .QueryFirstOrDefaultAsync<BovedaAbiertaDto>(
                new CommandDefinition(sql, new { OficinaId = oficinaId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }
}
