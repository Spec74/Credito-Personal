namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Ítem de planilla (paridad <c>CreditoController.PagoPlanillaDTO</c>).</summary>
public sealed record PagoPlanillaItemDto(
    int CreditoId,
    decimal MontoPagar,
    int TipoPagoId,
    string? FechaHoraTrans);

/// <summary>Cuerpo de <c>POST /api/v1/credito/cobrar-planilla-bloque</c>.</summary>
public sealed record CobrarPlanillaBloqueRequest(
    int OficinaId,
    int CajaDiarioId,
    IReadOnlyList<PagoPlanillaItemDto> Planilla);

/// <summary>Resultado atómico de la planilla.</summary>
public sealed record CobrarPlanillaBloqueResponse(
    bool Exito,
    string Mensaje,
    int PagosProcesados,
    int ImpagosCompletados);
