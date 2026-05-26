namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptCredito</c> (paridad con <c>usp_RptCredito_Result</c>).</summary>
public sealed class RptCreditoRowDto
{
    public string? Producto { get; set; }
    public string? Cliente { get; set; }
    public int CreditoId { get; set; }
    public DateTime? FechaDesembolso { get; set; }
    public DateTime? FechaVcto { get; set; }
    public string? FormaPago { get; set; }
    public int NumeroCuotas { get; set; }
    public decimal Interes { get; set; }
    public string? Estado { get; set; }
    public decimal MontoProducto { get; set; }
    public decimal MontoInicial { get; set; }
    public decimal MontoCredito { get; set; }
    public string? TipoGastoAdm { get; set; }
    public decimal MontoGastosAdm { get; set; }
    public decimal MontoDesembolso { get; set; }
    public string? Observacion { get; set; }
}
