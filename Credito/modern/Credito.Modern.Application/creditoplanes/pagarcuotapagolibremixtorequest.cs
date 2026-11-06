namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Línea de cobro mixto (p. ej. 10 Yape/BCP + 20 efectivo).</summary>
public sealed record PagoMedioLineaDto(
    int TipoPagoId,
    decimal Importe,
    string? FechaPagoTransferencia = null);

/// <summary>
/// Cuerpo de <c>POST /api/v1/credito/pagar-cuota-importe-libre-mixto</c>.
/// Ejecuta N veces <c>usp_PagarCuotaPagoLibre</c> en una sola transacción.
/// </summary>
public sealed record PagarCuotaPagoLibreMixtoRequest(
    int OficinaId,
    int CajaDiarioId,
    int CreditoId,
    IReadOnlyList<PagoMedioLineaDto> Medios);

/// <summary>Resultado con todos los movimientos CUO generados.</summary>
public sealed record PagoCajaMixtoResultResponse(
    int? ResultId,
    IReadOnlyList<int> MovimientoCajaIds);
