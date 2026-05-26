using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class TransferenciaReadService(IOptions<SqlDatabaseOptions> options) : ITransferenciaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<TransferenciaListPageDto> ListarAsync(
        int oficinaId,
        string? buscar,
        int almacenId,
        int articuloId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId));
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;
        var offset = (page - 1) * pageSize;

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var parameters = new DynamicParameters();
        parameters.Add("OficinaId", oficinaId);
        parameters.Add("AlmacenId", almacenId > 0 ? almacenId : (int?)null);
        parameters.Add("ArticuloId", articuloId > 0 ? articuloId : (int?)null);
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var buscarTrim = buscar?.Trim();
        if (!string.IsNullOrEmpty(buscarTrim) && DateTime.TryParse(buscarTrim, out var fechaBuscar))
        {
            parameters.Add("BuscarFecha", fechaBuscar.Date);
            parameters.Add("BuscarId", null);
        }
        else if (!string.IsNullOrEmpty(buscarTrim) && int.TryParse(buscarTrim, out var idBuscar))
        {
            parameters.Add("BuscarFecha", null);
            parameters.Add("BuscarId", idBuscar);
        }
        else
        {
            parameters.Add("BuscarFecha", null);
            parameters.Add("BuscarId", null);
        }

        const string fromWhere = """
            FROM ALMACEN.Transferencia AS t
            INNER JOIN ALMACEN.Almacen AS ao ON ao.AlmacenId = t.AlmacenOrigenId
            INNER JOIN ALMACEN.Almacen AS ad ON ad.AlmacenId = t.AlmacenDestinoId
            WHERE (ao.OficinaId = @OficinaId OR ad.OficinaId = @OficinaId)
              AND (@AlmacenId IS NULL OR t.AlmacenOrigenId = @AlmacenId OR t.AlmacenDestinoId = @AlmacenId)
              AND (@BuscarId IS NULL OR t.TransferenciaId = @BuscarId)
              AND (@BuscarFecha IS NULL OR CAST(t.Fecha AS date) = @BuscarFecha)
              AND (
                  @ArticuloId IS NULL
                  OR EXISTS (
                      SELECT 1
                      FROM ALMACEN.TransferenciaSerie AS ts
                      INNER JOIN ALMACEN.SerieArticulo AS sa ON sa.SerieArticuloId = ts.SerieArticuloId
                      WHERE ts.TransferenciaId = t.TransferenciaId AND sa.ArticuloId = @ArticuloId
                  )
              )
            """;

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) " + fromWhere,
                parameters,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var items = (await connection.QueryAsync<TransferenciaListRowDto>(
            new CommandDefinition(
                $"""
                 SELECT t.TransferenciaId,
                        ao.Denominacion AS AlmacenOrigen,
                        ad.Denominacion AS AlmacenDestino,
                        t.Fecha,
                        t.Estado
                 {fromWhere}
                 ORDER BY t.TransferenciaId DESC
                 OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
                 """,
                parameters,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        return new TransferenciaListPageDto(items, total, page, pageSize);
    }

    public async Task<TransferenciaCabeceraDto?> ObtenerCabeceraAsync(
        int transferenciaId,
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (transferenciaId < 1 || oficinaId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await connection.QueryFirstOrDefaultAsync<TransferenciaCabeceraDto>(
            new CommandDefinition(
                """
                SELECT t.TransferenciaId,
                       t.AlmacenOrigenId,
                       t.AlmacenDestinoId,
                       ao.Denominacion AS AlmacenOrigen,
                       ad.Denominacion AS AlmacenDestino,
                       t.Fecha,
                       t.Estado,
                       CASE WHEN t.Estado = 'P' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Editable
                FROM ALMACEN.Transferencia AS t
                INNER JOIN ALMACEN.Almacen AS ao ON ao.AlmacenId = t.AlmacenOrigenId
                INNER JOIN ALMACEN.Almacen AS ad ON ad.AlmacenId = t.AlmacenDestinoId
                WHERE t.TransferenciaId = @TransferenciaId
                  AND (ao.OficinaId = @OficinaId OR ad.OficinaId = @OficinaId);
                """,
                new { TransferenciaId = transferenciaId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TransferenciaDetalleLineaDto>> ListarDetalleAsync(
        int transferenciaId,
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (transferenciaId < 1 || oficinaId < 1)
        {
            return [];
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var tieneAcceso = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM ALMACEN.Transferencia AS t
                    INNER JOIN ALMACEN.Almacen AS ao ON ao.AlmacenId = t.AlmacenOrigenId
                    INNER JOIN ALMACEN.Almacen AS ad ON ad.AlmacenId = t.AlmacenDestinoId
                    WHERE t.TransferenciaId = @TransferenciaId
                      AND (ao.OficinaId = @OficinaId OR ad.OficinaId = @OficinaId)
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new { TransferenciaId = transferenciaId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (!tieneAcceso)
        {
            return [];
        }

        var rows = await connection.QueryAsync<TransferenciaDetalleLineaDto>(
            new CommandDefinition(
                """
                SELECT @TransferenciaId AS TransferenciaId,
                       sa.ArticuloId,
                       a.Denominacion AS Articulo,
                       COUNT(*) AS Cantidad,
                       STRING_AGG(sa.NumeroSerie, ', ') WITHIN GROUP (ORDER BY sa.NumeroSerie) AS Series
                FROM ALMACEN.TransferenciaSerie AS ts
                INNER JOIN ALMACEN.SerieArticulo AS sa ON sa.SerieArticuloId = ts.SerieArticuloId
                INNER JOIN MAESTRO.Articulo AS a ON a.ArticuloId = sa.ArticuloId
                WHERE ts.TransferenciaId = @TransferenciaId
                GROUP BY sa.ArticuloId, a.Denominacion
                ORDER BY sa.ArticuloId;
                """,
                new { TransferenciaId = transferenciaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
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
