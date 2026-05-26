using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

public sealed class OrdenVentaEnvioReadService(IOptions<SqlDatabaseOptions> options) : IOrdenVentaEnvioReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<OrdenVentaEnvioDto?> GetOrdenAsync(
        int ordenVentaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<OrdenVentaEnvioDto>(
            new CommandDefinition(
                """
                SELECT OrdenVentaId, OficinaId, PersonaId, TotalNeto, Estado, TipoVenta
                FROM VENTAS.OrdenVenta
                WHERE OrdenVentaId = @OrdenVentaId;
                """,
                new { OrdenVentaId = ordenVentaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> ListarDescripcionesDetalleActivasAsync(
        int ordenVentaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT Descripcion
                FROM VENTAS.OrdenVentaDet
                WHERE OrdenVentaId = @OrdenVentaId
                  AND Estado = CAST(1 AS bit)
                ORDER BY OrdenVentaDetId;
                """,
                new { OrdenVentaId = ordenVentaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<bool> ExisteCreditoVinculadoAsync(
        int ordenVentaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM CREDITO.Credito WHERE OrdenVentaId = @OrdenVentaId
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new { OrdenVentaId = ordenVentaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
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
