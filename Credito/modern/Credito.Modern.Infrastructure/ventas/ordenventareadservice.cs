using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

public sealed class OrdenVentaReadService(IOptions<SqlDatabaseOptions> options) : IOrdenVentaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<OrdenVentaListPageDto> ListarAsync(
        int oficinaId,
        bool entregado,
        string? buscar,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
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

        var estadoFilter = entregado
            ? "ov.Estado IN ('ENT', 'ANU')"
            : "ov.Estado IN ('PEN', 'ENV')";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var where = $"""
            ov.OficinaId = @OficinaId
            AND {estadoFilter}
            """;

        if (fechaBuscar is not null)
        {
            where += " AND CAST(ov.FechaReg AS date) = @FechaBuscar";
        }
        else if (clave is not null)
        {
            where += """
                 AND (
                    CAST(ov.OrdenVentaId AS varchar(20)) LIKE '%' + @Clave + '%'
                    OR p.NombreCompleto LIKE '%' + @Clave + '%'
                 )
                """;
        }

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"""
                SELECT COUNT(*)
                FROM VENTAS.OrdenVenta AS ov
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = ov.PersonaId
                WHERE {where};
                """,
                new { OficinaId = oficinaId, Clave = clave, FechaBuscar = fechaBuscar },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var rows = await connection.QueryAsync<OrdenVentaListRowInternal>(
            new CommandDefinition(
                $"""
                SELECT ov.OrdenVentaId,
                       ov.FechaReg,
                       p.NombreCompleto AS Cliente,
                       ov.TotalDescuento,
                       ov.TotalNeto,
                       ov.TipoVenta,
                       ov.Estado,
                       c.Estado AS EstadoCredito
                FROM VENTAS.OrdenVenta AS ov
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = ov.PersonaId
                LEFT JOIN CREDITO.Credito AS c ON c.OrdenVentaId = ov.OrdenVentaId
                WHERE {where}
                ORDER BY ov.FechaReg DESC, ov.OrdenVentaId DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """,
                new
                {
                    OficinaId = oficinaId,
                    Clave = clave,
                    FechaBuscar = fechaBuscar,
                    Skip = (page - 1) * pageSize,
                    Take = pageSize,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var items = rows
            .Select(r => new OrdenVentaListRowDto(
                r.OrdenVentaId,
                r.FechaReg,
                r.Cliente,
                r.TotalDescuento,
                r.TotalNeto,
                r.TipoVenta,
                r.Estado,
                r.EstadoCredito,
                PuedeEliminar(r.Estado, r.TipoVenta, r.EstadoCredito)))
            .ToList();

        return new OrdenVentaListPageDto(items, totalCount, page, pageSize);
    }

    public async Task<OrdenVentaDetalleResponse?> ObtenerDetalleAsync(
        int ordenVentaId,
        CancellationToken cancellationToken = default)
    {
        if (ordenVentaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ordenVentaId), "ordenVentaId debe ser >= 1.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cabecera = await connection.QueryFirstOrDefaultAsync<OrdenVentaCabeceraInternal>(
            new CommandDefinition(
                """
                SELECT ov.OrdenVentaId,
                       ov.OficinaId,
                       ov.PersonaId,
                       p.NombreCompleto AS Cliente,
                       ov.Subtotal,
                       ov.TotalImpuesto,
                       ov.TotalNeto,
                       ov.TotalDescuento,
                       ov.Estado,
                       ov.TipoVenta,
                       ov.FechaReg,
                       c.Estado AS EstadoCredito
                FROM VENTAS.OrdenVenta AS ov
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = ov.PersonaId
                LEFT JOIN CREDITO.Credito AS c ON c.OrdenVentaId = ov.OrdenVentaId
                WHERE ov.OrdenVentaId = @OrdenVentaId;
                """,
                new { OrdenVentaId = ordenVentaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (cabecera is null)
        {
            return null;
        }

        var detalle = await connection.QueryAsync<OrdenVentaDetLineaDto>(
            new CommandDefinition(
                """
                SELECT OrdenVentaDetId,
                       ArticuloId,
                       Cantidad,
                       Descripcion,
                       ValorVenta,
                       Descuento,
                       Subtotal,
                       Estado
                FROM VENTAS.OrdenVentaDet
                WHERE OrdenVentaId = @OrdenVentaId
                ORDER BY OrdenVentaDetId;
                """,
                new { OrdenVentaId = ordenVentaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var lineas = detalle.ToList();
        var cabeceraDto = new OrdenVentaCabeceraDto(
            cabecera.OrdenVentaId,
            cabecera.OficinaId,
            cabecera.PersonaId,
            cabecera.Cliente,
            cabecera.Subtotal,
            cabecera.TotalImpuesto,
            cabecera.TotalNeto,
            cabecera.TotalDescuento,
            cabecera.Estado,
            cabecera.TipoVenta,
            cabecera.FechaReg,
            cabecera.EstadoCredito,
            PuedeEliminar(cabecera.Estado, cabecera.TipoVenta, cabecera.EstadoCredito));

        return new OrdenVentaDetalleResponse(
            cabeceraDto,
            lineas,
            lineas.Where(x => x.Estado).Sum(x => x.Cantidad));
    }

    internal static bool PuedeEliminar(string estado, string tipoVenta, string? estadoCredito)
    {
        if (string.Equals(estado, "ENT", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(estado, "PEN", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(tipoVenta, "CON", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(tipoVenta, "CRE", StringComparison.OrdinalIgnoreCase)
               && string.Equals(estadoCredito, "CRE", StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class OrdenVentaListRowInternal
    {
        public int OrdenVentaId { get; init; }
        public DateTime FechaReg { get; init; }
        public string Cliente { get; init; } = string.Empty;
        public decimal TotalDescuento { get; init; }
        public decimal TotalNeto { get; init; }
        public string TipoVenta { get; init; } = string.Empty;
        public string Estado { get; init; } = string.Empty;
        public string? EstadoCredito { get; init; }
    }

    private sealed class OrdenVentaCabeceraInternal
    {
        public int OrdenVentaId { get; init; }
        public int OficinaId { get; init; }
        public int PersonaId { get; init; }
        public string Cliente { get; init; } = string.Empty;
        public decimal Subtotal { get; init; }
        public decimal TotalImpuesto { get; init; }
        public decimal TotalNeto { get; init; }
        public decimal TotalDescuento { get; init; }
        public string Estado { get; init; } = string.Empty;
        public string TipoVenta { get; init; } = string.Empty;
        public DateTime FechaReg { get; init; }
        public string? EstadoCredito { get; init; }
    }
}
