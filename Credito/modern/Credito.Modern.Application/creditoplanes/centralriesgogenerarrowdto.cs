namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_CentralRiesgoGenerar</c> (paridad con <c>usp_CentralRiesgoGenerar_Result</c>).</summary>
public sealed class CentralRiesgoGenerarRowDto
{
    public int? Anio { get; set; }
    public int? Mes { get; set; }
    public int CreditoId { get; set; }
    public string? Periodo { get; set; }
    public string? Entidad { get; set; }
    public int TipoDoc { get; set; }
    public string? NumDoc { get; set; }
    public string? RazonSocial { get; set; }
    public string? ApePat { get; set; }
    public string? ApeMat { get; set; }
    public string? Nombres { get; set; }
    public int TipoPersona { get; set; }
    public int ModalidadCredito { get; set; }
    public string? DeudaMenor30 { get; set; }
    public string? DeudaMayor30 { get; set; }
    public int Calificacion { get; set; }
    public int DiasAtrazo { get; set; }
    public string? Direccion { get; set; }
    public string? celular { get; set; }
}
