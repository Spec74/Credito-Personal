namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptMovimientoBoveda</c> (paridad con <c>usp_RptMovimientoBoveda_Result</c>).</summary>
public sealed class RptMovimientoBovedaRowDto
{
    public int MovimientoBovedaId { get; set; }
    public DateTime FechaReg { get; set; }
    public string? CodOperacion { get; set; }
    public string? Glosa { get; set; }
    public decimal? Entrada { get; set; }
    public decimal? Salida { get; set; }
    public string? TipoPago { get; set; }
    public string? Agente { get; set; }
}
