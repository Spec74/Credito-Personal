namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptCreditosActivos</c> (paridad con <c>usp_RptCreditosActivos_Result</c>).</summary>
public sealed class RptCreditosActivosRowDto
{
    public long? Nro { get; set; }
    public string? Estado { get; set; }
    public string? Agente { get; set; }
    public int CreditoId { get; set; }
    public string? Codigo { get; set; }
    public string? Cliente { get; set; }
    public decimal MontoCredito { get; set; }
    public string? FormaPago { get; set; }
    public int NumeroCuotas { get; set; }
    public decimal Interes { get; set; }
    public decimal? MontoInteres { get; set; }
    public decimal? MontoCreditoTotal { get; set; }
    public decimal MontoGastosAdm { get; set; }
    public decimal CentralRiesgo { get; set; }
    public DateTime FechaPrimerPago { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public int? NroCuotasPagado { get; set; }
    public decimal? Pagado { get; set; }
    public decimal InteresPagado { get; set; }
    public int? NroCuotasPen { get; set; }
    public decimal? SaldoCapital { get; set; }
    public decimal? SaldoInteres { get; set; }
    public decimal? Saldo { get; set; }
    public int? DiasAtrazo { get; set; }
    public decimal? Mora { get; set; }
}
