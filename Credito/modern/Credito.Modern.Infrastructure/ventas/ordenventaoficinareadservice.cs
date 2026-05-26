using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

public sealed class OrdenVentaOficinaReadService(IOptions<SqlDatabaseOptions> options) : IOrdenVentaOficinaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<int?> GetOficinaIdByOrdenVentaIdAsync(
        int ordenVentaId,
        CancellationToken cancellationToken = default)
    {
        if (ordenVentaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ordenVentaId), "ordenVentaId debe ser >= 1.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SELECT OficinaId FROM VENTAS.OrdenVenta WHERE OrdenVentaId = @OrdenVentaId;",
                new { OrdenVentaId = ordenVentaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
