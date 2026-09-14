namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Cabecera del reporte de saldos de caja (paridad <c>CajaDiarioBL.ObtenerRptSaldoCajaCab</c> y
/// <c>CajaChicaDiarioBL.ObtenerRptSaldoCajaChicaCab</c>), que en el legado alimentaba los
/// parámetros de <c>rptSaldoCaja.rdlc</c>.
/// </summary>
public sealed class RptSaldoCajaCabDto
{
    public string Oficina { get; init; } = string.Empty;

    /// <summary>Usuario, nombre completo y caja, como en el RDLC.</summary>
    public string Cajero { get; init; } = string.Empty;

    public string Estado { get; init; } = string.Empty;

    public DateTime Fecha { get; init; }

    public decimal SaldoInicial { get; init; }

    public decimal SaldoFinal { get; init; }

    public decimal PorcentajeCobro { get; init; }
}
