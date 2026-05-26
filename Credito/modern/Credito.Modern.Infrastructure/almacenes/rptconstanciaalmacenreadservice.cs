using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class RptConstanciaAlmacenReadService(IOptions<SqlDatabaseOptions> options)
    : IRptConstanciaAlmacenReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<RptConstanciaAlmacenDto?> ObtenerAsync(
        int movimientoId,
        CancellationToken cancellationToken = default)
    {
        if (movimientoId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cabecera = await connection.QueryFirstOrDefaultAsync<RptConstanciaAlmacenCabeceraDto>(
            new CommandDefinition(
                """
                SELECT
                    m.MovimientoId,
                    o.Denominacion AS Oficina,
                    a.Denominacion AS Almacen,
                    CASE WHEN tm.IndEntrada = CAST(1 AS bit) THEN 'ENTRADA' ELSE 'SALIDA' END AS Tipo,
                    tm.Denominacion AS TipoMovimiento,
                    tm.Descripcion AS TipoMovimientoDesc,
                    m.Fecha,
                    m.Documento,
                    vt.Denominacion AS Estado,
                    m.Observacion,
                    m.TotalImporte AS Importe
                FROM ALMACEN.Movimiento AS m
                INNER JOIN ALMACEN.Almacen AS a ON a.AlmacenId = m.AlmacenId
                INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = a.OficinaId
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

        var detalle = await connection.QueryAsync<RptConstanciaAlmacenDetLineaDto>(
            new CommandDefinition(
                """
                SELECT
                    md.Cantidad,
                    md.Descripcion,
                    md.PrecioUnitario,
                    md.Descuento,
                    md.Importe
                FROM ALMACEN.MovimientoDet AS md
                WHERE md.MovimientoId = @MovimientoId
                ORDER BY md.MovimientoDetId;
                """,
                new { MovimientoId = movimientoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return new RptConstanciaAlmacenDto(cabecera, detalle.ToList());
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
