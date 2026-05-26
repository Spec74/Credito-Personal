namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptCajaDiario</c> (paridad con <c>usp_RptCajaDiario_Result</c>).</summary>
public sealed class RptCajaDiarioRowDto
{
    public int CajaDiarioId { get; set; }
    public string? Oficina { get; set; }
    public string? Caja { get; set; }
    public string? Agente { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal Entradas { get; set; }
    public decimal Salidas { get; set; }
    public decimal SaldoFinal { get; set; }
    public DateTime FechaIniOperacion { get; set; }
    public DateTime? FechaFinOperacion { get; set; }
}
