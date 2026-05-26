namespace Credito.Modern.Application.CreditoTareas;

/// <summary>Paridad <c>TareaBL.ListarTareasParaReporte</c> / dsTareas.</summary>
public sealed class TareaReporteRowDto
{
    public int Nro { get; set; }
    public int TareaId { get; set; }
    public int CreditoId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteDni { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Analista { get; set; } = string.Empty;
    public string SubtareasResumen { get; set; } = string.Empty;
    public string DetalleSubtareas { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
