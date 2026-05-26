using Credito.Modern.Application.Marcas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Marcas;

public sealed class MarcaReadService(IOptions<SqlDatabaseOptions> options) : IMarcaReadService
{
    private const string SqlActivas = """
        SELECT m.MarcaId,
               m.Denominacion,
               m.Estado
        FROM MAESTRO.Marca AS m
        WHERE m.Estado = CAST(1 AS bit)
        ORDER BY m.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<MarcaListItemDto>> GetActivasAsync(CancellationToken cancellationToken = default)
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

        var rows = await connection.QueryAsync<MarcaListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<List<MarcaListItemDto>> ListAsync(
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
              SELECT m.MarcaId, m.Denominacion, m.Estado
              FROM MAESTRO.Marca AS m
              ORDER BY m.Denominacion;
              """
            : SqlActivas;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<MarcaListItemDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }
}
