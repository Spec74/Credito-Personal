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

        var row = await connection
            .QueryFirstOrDefaultAsync<BovedaAbiertaDto>(
                new CommandDefinition(sql, new { OficinaId = oficinaId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        if (row is null)
        {
            return null;
        }

        var sumaMedios = await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(
                BovedaCuadreSql.SumaMedios,
                new { BovedaId = row.BovedaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        var delta = decimal.Round(sumaMedios - row.SaldoFinal, 2);
        return row with { SumaMedios = sumaMedios, DeltaMedios = delta };
    }
}
