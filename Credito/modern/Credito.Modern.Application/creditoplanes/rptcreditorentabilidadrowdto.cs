namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptCreditoRentabilidad</c> (paridad con <c>usp_RptCreditoRentabilidad_Result</c>).</summary>
public sealed class RptCreditoRentabilidadRowDto
{
    public int CreditoId { get; set; }
    public string? Oficina { get; set; }
    public string? Codigo { get; set; }
    public string? Cliente { get; set; }
    public DateTime? FechaDesembolso { get; set; }
    public DateTime? FechaPago { get; set; }
    public int NumeroCuotas { get; set; }
    public string? FormaPago { get; set; }
    public string? Estado { get; set; }
    public decimal MontoCredito { get; set; }
    public decimal Interes { get; set; }
    public decimal? SumCuota { get; set; }
    public int? CuotasPagadas { get; set; }
    public decimal MontoGastosAdm { get; set; }
    public decimal? SumInteres { get; set; }
    public decimal? SumMora { get; set; }
    public decimal? SumPago { get; set; }
}
