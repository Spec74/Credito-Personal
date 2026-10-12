namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Ítem de planilla (paridad <c>CreditoController.PagoPlanillaDTO</c>).</summary>
public sealed record PagoPlanillaItemDto(
    int CreditoId,
    decimal MontoPagar,
    int TipoPagoId,
    string? FechaHoraTrans);

/// <summary>Cuerpo de <c>POST /api/v1/credito/cobrar-planilla-bloque</c>.</summary>
/// <param name="CompletarImpagos">
/// Si es <c>true</c>, tras los pagos ejecuta <c>usp_CompletarImpagos</c> (paridad MVC antigua).
/// El cobro PWA moderno envía <c>false</c>: los impagos se cierran solo desde Caja Diario.
/// </param>
public sealed record CobrarPlanillaBloqueRequest(
    int OficinaId,
    int CajaDiarioId,
    IReadOnlyList<PagoPlanillaItemDto> Planilla,
    bool CompletarImpagos = false);

/// <summary>Resultado atómico de la planilla.</summary>
public sealed record CobrarPlanillaBloqueResponse(
    bool Exito,
    string Mensaje,
    int PagosProcesados,
    int ImpagosCompletados);
