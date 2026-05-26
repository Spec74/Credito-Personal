namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptCreditoMorosidad</c> (paridad con <c>usp_RptCreditoMorosidad_Result</c>).</summary>
public sealed class RptCreditoMorosidadRowDto
{
    public int CreditoId { get; set; }
    public string? Cliente { get; set; }
    public string? Direccion { get; set; }
    public string? Celular { get; set; }
    public DateTime? FechaDesembolso { get; set; }
    public DateTime? FechaVcto { get; set; }
    public string? Articulo { get; set; }
    public decimal MontoCredito { get; set; }
    public decimal? SaldoCredito { get; set; }
    public DateTime? FechaUltPago { get; set; }
    public decimal? CapitalAtrazo { get; set; }
    public decimal? GA { get; set; }
    public decimal? InteresAtrazo { get; set; }
    public decimal? Mora { get; set; }
    public decimal? ImporteLibre { get; set; }
    public int? DiasAtrazo { get; set; }
    public int? CuotasAtrazo { get; set; }
    public decimal? DeudaAtrazo { get; set; }
}
