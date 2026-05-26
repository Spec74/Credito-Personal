using Credito.Modern.Application.ListaPrecios;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.ListaPrecios;

public sealed class ListaPrecioReadService(IOptions<SqlDatabaseOptions> options) : IListaPrecioReadService
{
    private const string Sql = """
        SELECT lp.ListaPrecioId,
               lp.ArticuloId,
               lp.Monto,
               lp.Descuento,
               lp.Estado,
               lp.Puntos,
               lp.PuntosCanje
        FROM VENTAS.ListaPrecio AS lp
        WHERE lp.Estado = CAST(1 AS bit)
          AND (@ArticuloId IS NULL OR lp.ArticuloId = @ArticuloId)
        ORDER BY lp.ArticuloId, lp.ListaPrecioId;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<ListaPrecioListItemDto>> GetActivosAsync(int? articuloId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        int? filtro = articuloId is >= 1 ? articuloId : null;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(Sql, new { ArticuloId = filtro }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ListaPrecioListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<List<ListaPrecioListItemDto>> ListAsync(
        int? articuloId,
        bool incluirInactivos,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        int? filtro = articuloId is >= 1 ? articuloId : null;
        var sql = incluirInactivos
            ? """
              SELECT lp.ListaPrecioId,
                     lp.ArticuloId,
                     lp.Monto,
                     lp.Descuento,
                     lp.Estado,
                     lp.Puntos,
                     lp.PuntosCanje
              FROM VENTAS.ListaPrecio AS lp
              WHERE (@ArticuloId IS NULL OR lp.ArticuloId = @ArticuloId)
              ORDER BY lp.ArticuloId, lp.ListaPrecioId;
              """
            : Sql;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<ListaPrecioListItemDto>(
            new CommandDefinition(sql, new { ArticuloId = filtro }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.ToList();
    }
}
