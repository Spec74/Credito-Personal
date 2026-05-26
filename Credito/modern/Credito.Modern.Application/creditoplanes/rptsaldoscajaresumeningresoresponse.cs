namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Primer valor textual de <c>CREDITO.usp_RptSaldosCajaResumenIngreso</c> (paridad con <c>CajaDiarioBL.ObtenerResumenIngresoCajaDiario</c>, que usa <c>.ToList()[0]</c>).</summary>
public sealed record RptSaldosCajaResumenIngresoResponse(string? Texto);
