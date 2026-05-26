namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Paridad <c>CajaDiarioController.ProcesarPagoCuotaConMora</c>.</summary>
public sealed record PagarCuotaConMoraRequest(
    int OficinaId,
    int CajaDiarioId,
    int CreditoId,
    decimal ImporteRecibido,
    bool EsUltimaCuota,
    int TipoPagoId = 1,
    string? FechaPagoTransferencia = null);

public sealed record PagarCuotaConMoraResponse(
    int? MovimientoCajaId,
    string Mensaje,
    bool MoraLiquidada);
