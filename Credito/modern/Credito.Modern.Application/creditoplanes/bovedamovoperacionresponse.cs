namespace Credito.Modern.Application.CreditoPlanes;

public sealed record BovedaMovOperacionResponse(
    int MovimientoBovedaId,
    int? MovimientoCajaId = null);
