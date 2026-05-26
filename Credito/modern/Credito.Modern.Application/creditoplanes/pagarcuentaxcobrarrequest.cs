namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/pagar-cuenta-por-cobrar</c>.</summary>
public sealed record PagarCuentaxCobrarRequest(
    int OficinaId,
    int CajaDiarioId,
    int OrdenVentaId,
    int CuentaxCobrarId);
