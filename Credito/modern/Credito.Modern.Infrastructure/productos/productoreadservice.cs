using Credito.Modern.Application.Productos;
using Credito.Modern.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Productos;

public sealed class ProductoReadService(IOptions<SqlDatabaseOptions> options) : IProductoReadService
{
    private const string SqlActivos = """
        SELECT p.ProductoId,
               p.Denominacion,
               p.InteresMinima,
               p.InteresMaxima,
               p.DiasGracia,
               p.ImporteMoratorio,
               p.Estado,
               p.IndMora
        FROM CREDITO.Producto AS p
        WHERE p.Estado = CAST(1 AS bit)
        ORDER BY p.Denominacion;
        """;

    private const string SqlActivoById = """
        SELECT p.ProductoId,
               p.Denominacion,
               p.InteresMinima,
               p.InteresMaxima,
               p.DiasGracia,
               p.ImporteMoratorio,
               p.Estado,
               p.IndMora
        FROM CREDITO.Producto AS p
        WHERE p.Estado = CAST(1 AS bit)
          AND p.ProductoId = @ProductoId;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<ProductoListItemDto>> GetActivosAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(
            SqlActivos,
            parameters: null,
            transaction: null,
            commandTimeout: 30,
            commandType: System.Data.CommandType.Text,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<ProductoListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<ProductoListItemDto?> GetActivoByIdAsync(
        int productoId,
        CancellationToken cancellationToken = default)
    {
        if (productoId < 1)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await connection.QueryFirstOrDefaultAsync<ProductoListItemDto>(
                new CommandDefinition(
                    SqlActivoById,
                    new { ProductoId = productoId },
                    commandTimeout: 30,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }
}
