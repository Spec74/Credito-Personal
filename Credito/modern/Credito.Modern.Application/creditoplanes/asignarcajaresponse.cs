namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Resultado de asignar caja. <c>CajaDiarioId</c> solo para caja diario (no chica).</summary>
public sealed record AsignarCajaResponse(int? CajaDiarioId, bool EsCajaChica);
