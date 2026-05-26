namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Resultado de <c>CREDITO.usp_ObtenerMontoPendientePlanPago</c> (paridad con <c>BovedaBL</c>).</summary>
public sealed record ObtenerMontoPendientePlanPagoResponse(decimal? MontoPendiente);
