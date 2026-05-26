namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Respuesta estándar de operaciones de ciclo de crédito vía <c>usp_*</c>.</summary>
public sealed record CreditoCicloOperacionResponse(int CreditoId, bool Ok = true);
