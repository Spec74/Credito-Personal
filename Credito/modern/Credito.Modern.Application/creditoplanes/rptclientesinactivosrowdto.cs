namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptClientesInactivos</c> (paridad con <c>usp_RptClientesInactivos_Result</c>).</summary>
public sealed class RptClientesInactivosRowDto
{
    public int PersonaId { get; set; }
    public string? Agente { get; set; }
    public string? Codigo { get; set; }
    public string? Dni { get; set; }
    public string? Cliente { get; set; }
    public string? Direccion { get; set; }
    public string? DireccionRef { get; set; }
    public string? Celular { get; set; }
    public string? Calificacion { get; set; }
    public string? DireccionNegocio { get; set; }
    public string? DireccionNegocioRef { get; set; }
}
