namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Resultado de procs de caja diario que devuelven <c>int</c> en EF.</summary>
public sealed record CajaDiarioOperacionResponse(int ResultCode, int? CajaDiarioId = null);
