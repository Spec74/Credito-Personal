namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptCreditosMorososPagados</c> (paridad con <c>usp_RptCreditosMorososPagados_Result</c>).</summary>
public sealed class RptCreditosMorososPagadosRowDto
{
    public int CreditoId { get; set; }
    public string? Cliente { get; set; }
    public decimal MontoCredito { get; set; }
    public decimal Interes { get; set; }
    public string? FormaPago { get; set; }
    public int NumeroCuotas { get; set; }
    public decimal MontoGastosAdm { get; set; }
    public decimal CentralRiesgo { get; set; }
    public DateTime FechaPrimerPago { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaPagado { get; set; }
    public string? Agente { get; set; }
}
