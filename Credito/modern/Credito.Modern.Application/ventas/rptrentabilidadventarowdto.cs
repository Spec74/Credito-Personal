namespace Credito.Modern.Application.Ventas;

/// <summary>Fila de <c>VENTAS.usp_RptRentabilidadVenta</c> (paridad con <c>usp_RptRentabilidadVenta_Result</c>).</summary>
public sealed class RptRentabilidadVentaRowDto
{
    public long? Nro { get; set; }
    public string? Codigo { get; set; }
    public string? Articulo { get; set; }
    public int? MovimientoId { get; set; }
    public DateTime? FechaEnt { get; set; }
    public decimal? PrecioEnt { get; set; }
    public int? OrdenVentaId { get; set; }
    public DateTime? FechaSal { get; set; }
    public decimal? PrecioSal { get; set; }
    public string? Modalidad { get; set; }
    public decimal? Rentabilidad { get; set; }
    public string? Cliente { get; set; }
}
