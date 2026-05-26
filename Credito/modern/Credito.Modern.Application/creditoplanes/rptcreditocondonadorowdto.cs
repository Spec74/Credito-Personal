namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila equivalente a <c>RptCreditoCondonado</c> en <c>CreditoBL.ReporteCreditoCondonado</c>.</summary>
public sealed class RptCreditoCondonadoRowDto
{
    public int OficinaId { get; set; }
    public string? Oficina { get; set; }
    public int CreditoId { get; set; }
    public string? Cliente { get; set; }
    public DateTime FechaPrimerPago { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal MontoCredito { get; set; }
    public decimal Interes { get; set; }
    public decimal MontoCondonado { get; set; }
    public int AgenteId { get; set; }
    public string? Agente { get; set; }
    public string? Observacion { get; set; }
}
