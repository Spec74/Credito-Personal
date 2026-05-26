using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

public sealed class VentaRapidaReadService(IOptions<SqlDatabaseOptions> options) : IVentaRapidaReadService
{
    private const int SerieEnAlmacen = 2;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<ArticuloVentaRapidaDto?> ObtenerPorCodigoAsync(
        string codArticulo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codArticulo))
        {
            throw new ArgumentOutOfRangeException(nameof(codArticulo), "codigo es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var row = await connection.QueryFirstOrDefaultAsync<ArticuloRow>(
            new CommandDefinition(
                """
                SELECT TOP (1)
                    a.ArticuloId,
                    a.CodArticulo,
                    a.Denominacion,
                    lp.Monto AS PrecioVenta
                FROM ALMACEN.Articulo AS a
                INNER JOIN VENTAS.ListaPrecio AS lp
                    ON lp.ArticuloId = a.ArticuloId AND lp.Estado = CAST(1 AS bit)
                WHERE a.CodArticulo = @CodArticulo
                  AND a.Estado = CAST(1 AS bit)
                ORDER BY lp.ListaPrecioId;
                """,
                new { CodArticulo = codArticulo.Trim() },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        var stock = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM ALMACEN.SerieArticulo
                WHERE ArticuloId = @ArticuloId
                  AND EstadoId = @EstadoId;
                """,
                new { row.ArticuloId, EstadoId = SerieEnAlmacen },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return new ArticuloVentaRapidaDto(
            row.ArticuloId,
            row.CodArticulo,
            row.Denominacion,
            row.PrecioVenta,
            stock);
    }

    private sealed class ArticuloRow
    {
        public int ArticuloId { get; init; }
        public string CodArticulo { get; init; } = string.Empty;
        public string Denominacion { get; init; } = string.Empty;
        public decimal? PrecioVenta { get; init; }
    }
}
