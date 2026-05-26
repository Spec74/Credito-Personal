namespace Credito.Modern.Application.Almacenes;

/// <summary>Fila de <c>ALMACEN.usp_GenerarKardex</c> (paridad con <c>usp_GenerarKardex_Result</c>).</summary>
public sealed class GenerarKardexRowDto
{
    public int? MovimientoDetId { get; set; }
    public DateTime? Fecha { get; set; }
    public string? Concepto { get; set; }
    public int? CantEnt { get; set; }
    public decimal? PUEnt { get; set; }
    public decimal? TotalEnt { get; set; }
    public int? CantSal { get; set; }
    public decimal? PUSal { get; set; }
    public decimal? TotalSal { get; set; }
    public int? CantSaldo { get; set; }
    public decimal? PUSaldo { get; set; }
    public decimal? TotalSaldo { get; set; }
}
