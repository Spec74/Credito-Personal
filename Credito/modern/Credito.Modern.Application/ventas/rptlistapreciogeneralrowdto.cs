namespace Credito.Modern.Application.Ventas;

/// <summary>Fila equivalente a <c>ReporteListaPrecioGeneral</c> en <c>ReporteBL.ListarReporteListaPrecio</c>.</summary>
public sealed class RptListaPrecioGeneralRowDto
{
    public int ArticuloId { get; set; }
    public string? TipoArticulo { get; set; }
    public string? ArticuloDes { get; set; }
    public decimal Monto { get; set; }
    public decimal? Descuento { get; set; }
    public int? PuntosCanje { get; set; }
}
