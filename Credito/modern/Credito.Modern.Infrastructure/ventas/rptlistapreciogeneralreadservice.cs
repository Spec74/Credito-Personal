using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

public sealed class RptListaPrecioGeneralReadService(IOptions<SqlDatabaseOptions> options)
    : IRptListaPrecioGeneralReadService
{
    private const string Sql = """
        SELECT lp.ArticuloId,
               ta.Denominacion AS TipoArticulo,
               a.Denominacion AS ArticuloDes,
               lp.Monto,
               lp.Descuento,
               lp.PuntosCanje
        FROM VENTAS.ListaPrecio AS lp
        INNER JOIN ALMACEN.Articulo AS a ON a.ArticuloId = lp.ArticuloId
        INNER JOIN MAESTRO.TipoArticulo AS ta ON ta.TipoArticuloId = a.TipoArticuloId
        INNER JOIN MAESTRO.Modelo AS mo ON mo.ModeloId = a.ModeloId
        WHERE lp.Estado = CAST(1 AS bit)
          AND (@MarcaId IS NULL OR mo.MarcaId = @MarcaId)
          AND (@IndDescuento = 0 OR lp.Descuento > 0)
          AND (@IndPuntos = 0 OR lp.PuntosCanje > 0)
        ORDER BY ta.Denominacion, a.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptListaPrecioGeneralRowDto>> ListarAsync(
        int? marcaId,
        bool indDescuento,
        bool indPuntos,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (marcaId is { } mid && mid < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(marcaId), "marcaId, si se indica, debe ser >= 1.");
        }

        int? filtroMarca = marcaId is >= 1 ? marcaId : null;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            Sql,
            new
            {
                MarcaId = filtroMarca,
                IndDescuento = indDescuento ? 1 : 0,
                IndPuntos = indPuntos ? 1 : 0,
            },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptListaPrecioGeneralRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
