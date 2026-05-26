namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/pagar-cuotas</c> (paridad <c>CajaDiarioBL.PagarCuotas</c>).</summary>
public sealed record PagarCuotasRequest(
    int OficinaId,
    int CajaDiarioId,
    int CreditoId,
    string ListaPlanPagoId,
    decimal ImporteRecibido,
    int TipoPagoId = 1,
    string? FechaPagoTransferencia = null,
    /// <summary>Si true, liquida moras acumuladas en CreditoMora (última cuota pendiente).</summary>
    bool EsUltimaCuota = false,
    /// <summary>Si true, registra moras postergadas tras el pago (producto IndMora).</summary>
    bool AplicarMoraPostergada = true);
