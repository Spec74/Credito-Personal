namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptMovimientoCajaAnulado</c> (paridad con <c>usp_RptMovimientoCajaAnulado_Result</c>).</summary>
public sealed class RptMovimientoCajaAnuladoRowDto
{
    public int MovimientoCajaId { get; set; }
    public string? Operacion { get; set; }
    public decimal ImportePago { get; set; }
    public string? Persona { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaReg { get; set; }
    public string? UsuarioRegistro { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? UsuarioAnulacion { get; set; }
}
