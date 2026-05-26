using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

public sealed class OrdenVentaDetScopeReadService(IOptions<SqlDatabaseOptions> options) : IOrdenVentaDetScopeReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<OrdenVentaDetScopeDto?> GetScopeByDetIdAsync(
        int ordenVentaDetId,
        CancellationToken cancellationToken = default)
    {
        if (ordenVentaDetId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ordenVentaDetId), "ordenVentaDetId debe ser >= 1.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<OrdenVentaDetScopeDto>(
            new CommandDefinition(
                """
                SELECT ovd.OrdenVentaDetId, ovd.OrdenVentaId, ov.OficinaId
                FROM VENTAS.OrdenVentaDet AS ovd
                INNER JOIN VENTAS.OrdenVenta AS ov ON ov.OrdenVentaId = ovd.OrdenVentaId
                WHERE ovd.OrdenVentaDetId = @OrdenVentaDetId;
                """,
                new { OrdenVentaDetId = ordenVentaDetId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
