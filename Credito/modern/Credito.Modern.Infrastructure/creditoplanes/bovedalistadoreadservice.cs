using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class BovedaListadoReadService(IOptions<SqlDatabaseOptions> options) : IBovedaListadoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<(IReadOnlyList<BovedaListadoRowDto> Rows, int Total)> ListarAsync(
        int oficinaId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var skip = (page - 1) * pageSize;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string countSql = """
            SELECT COUNT(1) FROM CREDITO.Boveda WHERE OficinaId = @OficinaId;
            """;

        const string sql = """
            SELECT
                b.BovedaId,
                CASE WHEN b.IndTemporal = CAST(1 AS bit) THEN 'TMP' ELSE 'PRI' END AS Tipo,
                b.SaldoInicial,
                b.Entradas,
                b.Salidas,
                b.SaldoFinal,
                b.FechaIniOperacion,
                b.FechaFinOperacion,
                b.IndCierre
            FROM CREDITO.Boveda AS b
            WHERE b.OficinaId = @OficinaId
            ORDER BY b.BovedaId DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, new { OficinaId = oficinaId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        var rows = await connection
            .QueryAsync<BovedaListadoRowDto>(
                new CommandDefinition(
                    sql,
                    new { OficinaId = oficinaId, Skip = skip, Take = pageSize },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return (rows.ToList(), total);
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
