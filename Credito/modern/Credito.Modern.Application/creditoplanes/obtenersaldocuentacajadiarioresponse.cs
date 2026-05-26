namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Resultado de <c>CREDITO.usp_ObtenerSaldoCuentaCajadiario</c> (paridad con <c>CajaDiarioBL.ObtenerSaldoCuentaCajadiario</c>).</summary>
public sealed record ObtenerSaldoCuentaCajaDiarioResponse(decimal? Saldo);
