namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Paridad <c>MovimientoRendidoCajaChicaBL.ReporteComprobantesCajaChica</c>.</summary>
public sealed class RptComprobantesCajaChicaRowDto
{
    public string? Gasto { get; set; }
    public DateTime Fecha { get; set; }
    public string? Documento { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public string? Ruc { get; set; }
    public string? RazonSocial { get; set; }
    public string? DetalleGasto { get; set; }
    public decimal Importe { get; set; }
}
