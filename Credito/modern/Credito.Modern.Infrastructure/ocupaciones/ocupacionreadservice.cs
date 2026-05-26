using Credito.Modern.Application.Ocupaciones;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ocupaciones;

public sealed class OcupacionReadService(IOptions<SqlDatabaseOptions> options) : IOcupacionReadService
{
    private const string Sql = """
        SELECT o.OcupacionId,
               o.Denominacion,
               o.Estado
        FROM MAESTRO.Ocupacion AS o
        WHERE o.Estado = CAST(1 AS bit)
        ORDER BY o.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<OcupacionListItemDto>> GetActivasAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(Sql, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<OcupacionListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
