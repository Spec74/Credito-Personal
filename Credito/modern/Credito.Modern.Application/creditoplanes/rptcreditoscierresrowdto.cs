namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptCreditosCierres</c> (paridad con <c>usp_RptCreditosCierres_Result</c>).</summary>
public sealed class RptCreditosCierresRowDto
{
    public int CreditoId { get; set; }
    public string? Estado { get; set; }
    public string? Agente { get; set; }
    public string? Codigo { get; set; }
    public string? Cliente { get; set; }
    public decimal MontoCredito { get; set; }
    public string? FormaPago { get; set; }
    public int NumeroCuotas { get; set; }
    public decimal Interes { get; set; }
    public decimal MontoGastosAdm { get; set; }
    public decimal CentralRiesgo { get; set; }
    public DateTime FechaPrimerPago { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal? SumAmortizacion { get; set; }
    public decimal? SumInteres { get; set; }
    public decimal? SumCuota { get; set; }
    public int? SumNroCuota { get; set; }
}
