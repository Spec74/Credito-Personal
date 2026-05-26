using Credito.Modern.Application.Articulos;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Articulos;

public sealed class ArticuloReadService(IOptions<SqlDatabaseOptions> options) : IArticuloReadService
{
    private const string Sql = """
        SELECT a.ArticuloId,
               a.ModeloId,
               a.TipoArticuloId,
               a.CodArticulo,
               a.Denominacion,
               a.Descripcion,
               a.IndPerecible,
               a.IndImportado,
               a.IndCanjeable,
               a.Estado
        FROM ALMACEN.Articulo AS a
        WHERE a.Estado = CAST(1 AS bit)
          AND (@ModeloId IS NULL OR a.ModeloId = @ModeloId)
          AND (@TipoArticuloId IS NULL OR a.TipoArticuloId = @TipoArticuloId)
        ORDER BY a.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<ArticuloListItemDto>> GetActivosAsync(
        int? modeloId,
        int? tipoArticuloId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        int? filtroModelo = modeloId is >= 1 ? modeloId : null;
        int? filtroTipo = tipoArticuloId is >= 1 ? tipoArticuloId : null;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            Sql,
            new { ModeloId = filtroModelo, TipoArticuloId = filtroTipo },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ArticuloListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<List<ArticuloGestionListItemDto>> ListGestionAsync(
        int? modeloId,
        int? tipoArticuloId,
        bool incluirInactivos,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        int? filtroModelo = modeloId is >= 1 ? modeloId : null;
        int? filtroTipo = tipoArticuloId is >= 1 ? tipoArticuloId : null;
        var estadoFilter = incluirInactivos ? "" : "AND a.Estado = CAST(1 AS bit)";

        var sql = $"""
            SELECT a.ArticuloId,
                   a.ModeloId,
                   m.Denominacion AS ModeloDenominacion,
                   a.TipoArticuloId,
                   t.Denominacion AS TipoArticuloDenominacion,
                   a.CodArticulo,
                   a.Denominacion,
                   a.Descripcion,
                   a.IndPerecible,
                   a.IndImportado,
                   a.IndCanjeable,
                   a.Estado,
                   lp.Monto,
                   lp.Descuento
            FROM ALMACEN.Articulo AS a
            LEFT JOIN MAESTRO.Modelo AS m ON m.ModeloId = a.ModeloId
            LEFT JOIN MAESTRO.TipoArticulo AS t ON t.TipoArticuloId = a.TipoArticuloId
            OUTER APPLY (
                SELECT TOP (1) lp2.Monto, lp2.Descuento
                FROM VENTAS.ListaPrecio AS lp2
                WHERE lp2.ArticuloId = a.ArticuloId
                ORDER BY lp2.ListaPrecioId
            ) AS lp
            WHERE (@ModeloId IS NULL OR a.ModeloId = @ModeloId)
              AND (@TipoArticuloId IS NULL OR a.TipoArticuloId = @TipoArticuloId)
              {estadoFilter}
            ORDER BY a.Denominacion;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<ArticuloGestionListItemDto>(
            new CommandDefinition(
                sql,
                new { ModeloId = filtroModelo, TipoArticuloId = filtroTipo },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<ArticuloDetalleDto?> GetDetalleAsync(
        int articuloId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        const string sql = """
            SELECT a.ArticuloId,
                   a.ModeloId,
                   a.TipoArticuloId,
                   a.CodArticulo,
                   a.Denominacion,
                   a.Descripcion,
                   a.IndPerecible,
                   a.IndImportado,
                   a.IndCanjeable,
                   a.Estado,
                   lp.Monto,
                   lp.Descuento,
                   lp.ListaPrecioId
            FROM ALMACEN.Articulo AS a
            OUTER APPLY (
                SELECT TOP (1) lp2.Monto, lp2.Descuento, lp2.ListaPrecioId
                FROM VENTAS.ListaPrecio AS lp2
                WHERE lp2.ArticuloId = a.ArticuloId
                ORDER BY lp2.ListaPrecioId
            ) AS lp
            WHERE a.ArticuloId = @ArticuloId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QuerySingleOrDefaultAsync<ArticuloDetalleDto>(
            new CommandDefinition(sql, new { ArticuloId = articuloId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<List<ArticuloBuscarItemDto>> BuscarSelectAsync(
        string term,
        bool soloActivos,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        var palabras = term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (palabras.Length == 0)
        {
            return [];
        }

        var whereParts = new List<string>();
        var parameters = new DynamicParameters();
        if (soloActivos)
        {
            whereParts.Add("a.Estado = CAST(1 AS bit)");
        }

        for (var i = 0; i < palabras.Length; i++)
        {
            var key = $"P{i}";
            whereParts.Add($"a.Denominacion LIKE '%' + @{key} + '%'");
            parameters.Add(key, palabras[i]);
        }

        var sql = $"""
            SELECT TOP (10) a.ArticuloId, a.Denominacion
            FROM ALMACEN.Articulo AS a
            WHERE {string.Join(" AND ", whereParts)}
            ORDER BY a.Denominacion;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<ArticuloBuscarItemDto>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<string?> ObtenerImagenCsvAsync(int articuloId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                "SELECT Imagen FROM ALMACEN.Articulo WHERE ArticuloId = @ArticuloId;",
                new { ArticuloId = articuloId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
