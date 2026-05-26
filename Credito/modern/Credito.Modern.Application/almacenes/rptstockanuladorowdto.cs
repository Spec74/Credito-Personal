namespace Credito.Modern.Application.Almacenes;

/// <summary>Fila equivalente a <c>ReporteStockAnulado</c> en <c>ReporteBL.ListarReporteStockAnulados</c>.</summary>
public sealed class RptStockAnuladoRowDto
{
    public int MovimientoId { get; set; }
    public string? Movimiento { get; set; }
    public string? Observacion { get; set; }
    public DateTime Fecha { get; set; }
    public int Cantidad { get; set; }
    public string? Detalle { get; set; }
}
