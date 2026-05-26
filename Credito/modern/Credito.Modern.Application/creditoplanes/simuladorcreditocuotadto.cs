namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila devuelta por <c>CREDITO.usp_SimuladorCredito</c> (paridad con <c>usp_SimuladorCredito_Result</c>).</summary>
public sealed class SimuladorCreditoCuotaDto
{
    public int? Numero { get; set; }
    public decimal? Capital { get; set; }
    public DateTime? FechaPago { get; set; }
    public decimal? Amortizacion { get; set; }
    public decimal? Interes { get; set; }
    public decimal? GastosAdm { get; set; }
    public decimal? Cuota { get; set; }
}
