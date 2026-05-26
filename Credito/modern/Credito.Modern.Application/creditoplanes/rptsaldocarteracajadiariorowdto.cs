namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptSaldoCarteraCajaDiario</c> (paridad con <c>usp_RptSaldoCarteraCajaDiario_Result</c>).</summary>
public sealed class RptSaldoCarteraCajaDiarioRowDto
{
    public int AgenteId { get; set; }
    public string? Oficina { get; set; }
    public string? Caja { get; set; }
    public string? Agente { get; set; }
    public DateTime? FechaCierreIni { get; set; }
    public decimal SalidasIni { get; set; }
    public decimal? MontoCobradoIni { get; set; }
    public decimal? PocentajeCobroIni { get; set; }
    public decimal SaldoCarteraSinMoraIni { get; set; }
    public int NroClientesCarteraSinMoraIni { get; set; }
    public decimal SaldoMoraCarteraIni { get; set; }
    public int NroClientesSaldoMoraCarteraIni { get; set; }
    public int NroClientesNuevosIni { get; set; }
    public decimal SaldoVencidoIni { get; set; }
    public decimal SaldoMorosidadIni { get; set; }
    public DateTime? FechaCierreFin { get; set; }
    public decimal SalidasFin { get; set; }
    public decimal MontoCobradoFin { get; set; }
    public decimal? PocentajeCobroFin { get; set; }
    public decimal SaldoCarteraSinMoraFin { get; set; }
    public int NroClientesCarteraSinMoraFin { get; set; }
    public decimal SaldoMoraCarteraFin { get; set; }
    public int NroClientesSaldoMoraCarteraFin { get; set; }
    public int NroClientesNuevosFin { get; set; }
    public decimal SaldoVencidoFin { get; set; }
    public decimal SaldoMorosidadFin { get; set; }
}
