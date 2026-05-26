namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Resultado de <c>CREDITO.usp_CalcularMoraPendiente</c> (paridad con legado <c>MovimientoCajaBL</c>).</summary>
public sealed record CalcularMoraPendienteResponse(decimal? MoraPendiente);
