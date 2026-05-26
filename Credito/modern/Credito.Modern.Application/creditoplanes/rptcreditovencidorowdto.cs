namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptCreditoVencido</c> (paridad con <c>usp_RptCreditoVencido_Result</c>).</summary>
public sealed class RptCreditoVencidoRowDto
{
    public string? Gestor { get; set; }
    public int CreditoId { get; set; }
    public string? Cliente { get; set; }
    public decimal MontoCredito { get; set; }
    public string? FormaPago { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal? CreditoVencido { get; set; }
    public string? VencidoMenor60 { get; set; }
    public string? VencidoMayor60 { get; set; }
    public string? VencidoIrrecuperable { get; set; }
}
