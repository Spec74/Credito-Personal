namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_PagosNoVerificados</c> (paridad con <c>usp_PagosNoVerificados_Result</c>).</summary>
public sealed class PagosNoVerificadosRowDto
{
    public int MovimientoCajaId { get; set; }
    public string? Cliente { get; set; }
    public string? Movimiento { get; set; }
    public decimal ImportePago { get; set; }
    public string? TipoPago { get; set; }
    public string? FechaTransferencia { get; set; }
    public string? Registro { get; set; }
}
