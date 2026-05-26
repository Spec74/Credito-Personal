namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptCreditoAprobacion</c> (paridad con <c>usp_RptCreditoAprobacion_Result</c>).</summary>
public sealed class RptCreditoAprobacionRowDto
{
    public int CreditoId { get; set; }
    public string? Oficina { get; set; }
    public string? Cliente { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public decimal MontoCredito { get; set; }
    public decimal Interes { get; set; }
    public int NumeroCuotas { get; set; }
    public decimal MontoDesembolso { get; set; }
    public string? Gestor { get; set; }
}
