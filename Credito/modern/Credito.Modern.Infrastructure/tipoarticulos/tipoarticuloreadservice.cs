using Credito.Modern.Application.TipoArticulos;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.TipoArticulos;

public sealed class TipoArticuloReadService(IOptions<SqlDatabaseOptions> options) : ITipoArticuloReadService
{
    private const string SqlActivas = """
        SELECT t.TipoArticuloId,
               t.Denominacion,
               t.Descripcion,
               t.IndTieneCodigo,
               t.Estado,
               t.IndMovimientoAlmacen
        FROM MAESTRO.TipoArticulo AS t
        WHERE t.Estado = CAST(1 AS bit)
        ORDER BY t.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<TipoArticuloListItemDto>> GetActivosAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(
            SqlActivas,
            parameters: null,
            transaction: null,
            commandTimeout: 30,
            commandType: System.Data.CommandType.Text,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<TipoArticuloListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<List<TipoArticuloListItemDto>> ListAsync(
        bool incluirInactivos,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        var sql = incluirInactivos
            ? """
              SELECT t.TipoArticuloId,
                     t.Denominacion,
                     t.Descripcion,
                     t.IndTieneCodigo,
                     t.Estado,
                     t.IndMovimientoAlmacen
              FROM MAESTRO.TipoArticulo AS t
              ORDER BY t.Denominacion;
              """
            : SqlActivas;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<TipoArticuloListItemDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }
}
