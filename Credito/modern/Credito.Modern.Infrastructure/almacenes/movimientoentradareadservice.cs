using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class MovimientoEntradaReadService(IOptions<SqlDatabaseOptions> options)
    : IMovimientoEntradaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<MovimientoEntradaListPageDto> ListarEntradasAsync(
        int oficinaId,
        int almacenId,
        string? buscar,
        int articuloId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || almacenId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId y almacenId deben ser >= 1.");
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        EnsureConnection();

        var clave = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim();
        DateTime? fechaBuscar = null;
        if (clave is not null && DateTime.TryParse(clave, out var parsed))
        {
            fechaBuscar = parsed.Date;
            clave = null;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        string countSql;
        string listSql;
        object parameters;

        if (articuloId < 1)
        {
            var where = """
                m.AlmacenId = @AlmacenId
                AND a.OficinaId = @OficinaId
                AND tm.IndEntrada = CAST(1 AS bit)
                """;
            if (fechaBuscar is not null)
            {
                where += " AND CAST(m.Fecha AS date) = @FechaBuscar";
            }
            else if (clave is not null)
            {
                where += """
                     AND (
                        CAST(m.MovimientoId AS varchar(20)) LIKE '%' + @Clave + '%'
                        OR ISNULL(m.Documento, '') LIKE '%' + @Clave + '%'
                     )
                    """;
            }

            countSql = $"""
                SELECT COUNT(*)
                FROM ALMACEN.Movimiento AS m
                INNER JOIN ALMACEN.Almacen AS a ON a.AlmacenId = m.AlmacenId
                INNER JOIN ALMACEN.TipoMovimiento AS tm ON tm.TipoMovimientoId = m.TipoMovimientoId
                INNER JOIN MAESTRO.ValorTabla AS vt ON vt.TablaId = 5 AND vt.ItemId = m.EstadoId
                WHERE {where};
                """;

            listSql = $"""
                SELECT m.MovimientoId,
                       CASE WHEN tm.IndEntrada = CAST(1 AS bit) THEN 'ENTRADA' ELSE 'SALIDA' END AS Tipo,
                       m.TipoMovimientoId,
                       tm.Denominacion AS TipoMovimiento,
                       m.Fecha,
                       m.Documento,
                       vt.Denominacion AS Estado,
                       m.Observacion
                FROM ALMACEN.Movimiento AS m
                INNER JOIN ALMACEN.Almacen AS a ON a.AlmacenId = m.AlmacenId
                INNER JOIN ALMACEN.TipoMovimiento AS tm ON tm.TipoMovimientoId = m.TipoMovimientoId
                INNER JOIN MAESTRO.ValorTabla AS vt ON vt.TablaId = 5 AND vt.ItemId = m.EstadoId
                WHERE {where}
                ORDER BY m.Fecha DESC, m.MovimientoId DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            parameters = new
            {
                OficinaId = oficinaId,
                AlmacenId = almacenId,
                Clave = clave,
                FechaBuscar = fechaBuscar,
                Skip = (page - 1) * pageSize,
                Take = pageSize,
            };
        }
        else
        {
            countSql = """
                SELECT COUNT(DISTINCT m.MovimientoId)
                FROM ALMACEN.MovimientoDet AS md
                INNER JOIN ALMACEN.Movimiento AS m ON m.MovimientoId = md.MovimientoId
                INNER JOIN ALMACEN.Almacen AS a ON a.AlmacenId = m.AlmacenId
                INNER JOIN ALMACEN.TipoMovimiento AS tm ON tm.TipoMovimientoId = m.TipoMovimientoId
                WHERE m.AlmacenId = @AlmacenId
                  AND a.OficinaId = @OficinaId
                  AND tm.IndEntrada = CAST(1 AS bit)
                  AND md.ArticuloId = @ArticuloId;
                """;

            listSql = """
                SELECT DISTINCT m.MovimientoId,
                       CASE WHEN tm.IndEntrada = CAST(1 AS bit) THEN 'ENTRADA' ELSE 'SALIDA' END AS Tipo,
                       m.TipoMovimientoId,
                       tm.Denominacion AS TipoMovimiento,
                       m.Fecha,
                       m.Documento,
                       vt.Denominacion AS Estado,
                       m.Observacion
                FROM ALMACEN.MovimientoDet AS md
                INNER JOIN ALMACEN.Movimiento AS m ON m.MovimientoId = md.MovimientoId
                INNER JOIN ALMACEN.Almacen AS a ON a.AlmacenId = m.AlmacenId
                INNER JOIN ALMACEN.TipoMovimiento AS tm ON tm.TipoMovimientoId = m.TipoMovimientoId
                INNER JOIN MAESTRO.ValorTabla AS vt ON vt.TablaId = 5 AND vt.ItemId = m.EstadoId
                WHERE m.AlmacenId = @AlmacenId
                  AND a.OficinaId = @OficinaId
                  AND tm.IndEntrada = CAST(1 AS bit)
                  AND md.ArticuloId = @ArticuloId
                ORDER BY m.Fecha DESC, m.MovimientoId DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            parameters = new
            {
                OficinaId = oficinaId,
                AlmacenId = almacenId,
                ArticuloId = articuloId,
                Skip = (page - 1) * pageSize,
                Take = pageSize,
            };
        }

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        var rows = await connection.QueryAsync<MovimientoEntradaListRowDto>(
            new CommandDefinition(listSql, parameters, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return new MovimientoEntradaListPageDto(rows.ToList(), total, page, pageSize);
    }

    public async Task<MovimientoEntradaDetalleResponse?> ObtenerEntradaAsync(
        int movimientoId,
        CancellationToken cancellationToken = default)
    {
        if (movimientoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoId), "movimientoId debe ser >= 1.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cabecera = await connection.QueryFirstOrDefaultAsync<MovimientoEntradaCabeceraDto>(
            new CommandDefinition(
                """
                SELECT m.MovimientoId,
                       m.AlmacenId,
                       a.OficinaId,
                       a.Denominacion AS Almacen,
                       CASE WHEN tm.IndEntrada = CAST(1 AS bit) THEN 'ENTRADA' ELSE 'SALIDA' END AS Tipo,
                       m.TipoMovimientoId,
                       tm.Denominacion AS TipoMovimiento,
                       m.Fecha,
                       m.Documento,
                       vt.Denominacion AS Estado,
                       m.EstadoId,
                       m.Observacion,
                       m.SubTotal,
                       m.IGV AS Igv,
                       m.AjusteRedondeo,
                       m.TotalImporte,
                       CASE WHEN m.EstadoId = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Editable
                FROM ALMACEN.Movimiento AS m
                INNER JOIN ALMACEN.Almacen AS a ON a.AlmacenId = m.AlmacenId
                INNER JOIN ALMACEN.TipoMovimiento AS tm ON tm.TipoMovimientoId = m.TipoMovimientoId
                INNER JOIN MAESTRO.ValorTabla AS vt ON vt.TablaId = 5 AND vt.ItemId = m.EstadoId
                WHERE m.MovimientoId = @MovimientoId;
                """,
                new { MovimientoId = movimientoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (cabecera is null)
        {
            return null;
        }

        var docs = await connection.QueryAsync<MovimientoDocRowDto>(
            new CommandDefinition(
                """
                SELECT d.MovimientoDocId,
                       d.TipoDocumentoId,
                       t.Denominacion AS TipoDocumento,
                       d.SerieDocumento,
                       d.NroDocumento,
                       CASE WHEN m.EstadoId = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS PuedeEliminar
                FROM ALMACEN.MovimientoDoc AS d
                INNER JOIN ALMACEN.Movimiento AS m ON m.MovimientoId = d.MovimientoId
                INNER JOIN MAESTRO.TipoDocumento AS t ON t.TipoDocumentoId = d.TipoDocumentoId
                WHERE d.MovimientoId = @MovimientoId
                ORDER BY d.TipoDocumentoId, d.MovimientoDocId;
                """,
                new { MovimientoId = movimientoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var detalle = await connection.QueryAsync<MovimientoEntradaDetLineaDto>(
            new CommandDefinition(
                """
                SELECT md.MovimientoDetId,
                       md.ArticuloId,
                       md.Cantidad,
                       md.UnidadMedidaT10,
                       vt.DesCorta AS UnidadMedida,
                       md.Descripcion,
                       md.PrecioUnitario,
                       md.Descuento,
                       md.Importe,
                       md.IndCorrelativo,
                       CASE WHEN m.EstadoId = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS PuedeEliminar
                FROM ALMACEN.MovimientoDet AS md
                INNER JOIN ALMACEN.Movimiento AS m ON m.MovimientoId = md.MovimientoId
                LEFT JOIN MAESTRO.ValorTabla AS vt ON vt.TablaId = 10 AND vt.ItemId = md.UnidadMedidaT10
                WHERE md.MovimientoId = @MovimientoId
                ORDER BY md.MovimientoDetId;
                """,
                new { MovimientoId = movimientoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return new MovimientoEntradaDetalleResponse(
            cabecera,
            docs.ToList(),
            detalle.ToList());
    }

    public async Task<bool> AlmacenPerteneceOficinaAsync(
        int almacenId,
        int oficinaId,
        CancellationToken cancellationToken = default) =>
        await ExistsAlmacenOficinaAsync(almacenId, oficinaId, cancellationToken).ConfigureAwait(false);

    public async Task<bool> TipoMovimientoEsEntradaAsync(
        int tipoMovimientoId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM ALMACEN.TipoMovimiento
                    WHERE TipoMovimientoId = @TipoMovimientoId
                      AND Estado = CAST(1 AS bit)
                      AND IndEntrada = CAST(1 AS bit)
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new { TipoMovimientoId = tipoMovimientoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<bool> ExistsAlmacenOficinaAsync(
        int almacenId,
        int oficinaId,
        CancellationToken cancellationToken)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM ALMACEN.Almacen
                    WHERE AlmacenId = @AlmacenId AND OficinaId = @OficinaId AND Estado = CAST(1 AS bit)
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new { AlmacenId = almacenId, OficinaId = oficinaId },
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
