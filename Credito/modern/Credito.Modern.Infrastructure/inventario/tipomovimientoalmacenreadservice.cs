using Credito.Modern.Application.Inventario;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Inventario;

public sealed class TipoMovimientoAlmacenReadService(IOptions<SqlDatabaseOptions> options)
    : ITipoMovimientoAlmacenReadService
{
    private const string Sql = """
        SELECT t.TipoMovimientoId,
               t.Denominacion,
               t.Descripcion,
               t.IndEntrada,
               t.IndTransferencia,
               t.IndDevolucion,
               t.Estado
        FROM MAESTRO.TipoMovimiento AS t
        WHERE t.Estado = CAST(1 AS bit)
        ORDER BY t.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<TipoMovimientoAlmacenListItemDto>> GetActivosAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(Sql, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<TipoMovimientoAlmacenListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
