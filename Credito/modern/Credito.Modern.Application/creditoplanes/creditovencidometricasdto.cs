namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.uspCreditoVencido</c> (paridad con <c>uspCreditoVencido_Result</c> / <c>BovedaBL.CreditoVencido</c>).</summary>
public sealed class CreditoVencidoMetricasDto
{
    public decimal? CreditoVencido { get; set; }
    public decimal? VencidoMenor60 { get; set; }
    public decimal? VencidoMayor60 { get; set; }
    public decimal? VencidoIrrecuperable { get; set; }
}
