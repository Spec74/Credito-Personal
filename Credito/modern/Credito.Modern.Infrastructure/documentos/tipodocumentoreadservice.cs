using Credito.Modern.Application.Documentos;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Documentos;

public sealed class TipoDocumentoReadService(IOptions<SqlDatabaseOptions> options) : ITipoDocumentoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<TipoDocumentoListItemDto>> GetActivosAsync(bool soloParaVenta, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        const string sqlBase = """
            SELECT t.TipoDocumentoId,
                   t.Denominacion,
                   t.IndVenta,
                   t.IndAlmacen,
                   t.IndAlmacenMov,
                   t.IndCajaChica,
                   t.Estado
            FROM MAESTRO.TipoDocumento AS t
            WHERE t.Estado = CAST(1 AS bit)
            """;

        var sql = soloParaVenta
            ? sqlBase + " AND t.IndVenta = CAST(1 AS bit)\nORDER BY t.Denominacion;"
            : sqlBase + "ORDER BY t.Denominacion;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(
            sql,
            parameters: null,
            transaction: null,
            commandTimeout: 30,
            commandType: System.Data.CommandType.Text,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<TipoDocumentoListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<List<TipoDocumentoListItemDto>> GetActivosAlmacenMovAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        const string sql = """
            SELECT t.TipoDocumentoId,
                   t.Denominacion,
                   t.IndVenta,
                   t.IndAlmacen,
                   t.IndAlmacenMov,
                   t.IndCajaChica,
                   t.Estado
            FROM MAESTRO.TipoDocumento AS t
            WHERE t.Estado = CAST(1 AS bit)
              AND t.IndAlmacen = CAST(1 AS bit)
              AND t.IndAlmacenMov = CAST(0 AS bit)
            ORDER BY t.Denominacion;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<TipoDocumentoListItemDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }
}
