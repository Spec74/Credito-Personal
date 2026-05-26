namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Resultado de <c>CREDITO.usp_CompletarImpagosValidacion</c> (paridad con <c>CreditoBL.CompletarImpagosValidar</c>).</summary>
public sealed record CompletarImpagosValidacionResponse(int? CantidadImpagosPendientes);
