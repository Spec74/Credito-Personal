namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Paridad <c>CreditoBL.ReportePlanPago</c> / dsSimuladorPlanPago.</summary>
public sealed class RptPlanPagosRowDto
{
    public int Numero { get; set; }
    public decimal Capital { get; set; }
    public DateTime FechaPago { get; set; }
    public decimal Amortizacion { get; set; }
    public decimal Interes { get; set; }
    public decimal GastosAdm { get; set; }
    public decimal Cuota { get; set; }
}
