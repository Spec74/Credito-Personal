namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila equivalente a <c>RptCreditoObservado</c> en <c>CreditoBL.ReporteCreditoObservado</c> (consulta SQL, no proc).</summary>
public sealed class RptCreditoObservadoRowDto
{
    public int OficinaId { get; set; }
    public string? Oficina { get; set; }
    public int CreditoId { get; set; }
    public string? Cliente { get; set; }
    public DateTime FechaPrimerPago { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal MontoCredito { get; set; }
    public decimal Interes { get; set; }
    public int AgenteId { get; set; }
    public string? Agente { get; set; }
    public string? Observacion { get; set; }
    public decimal TramiteAdm { get; set; }
    public decimal CentralRiesgo { get; set; }
}
