namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Saldo disponible de un medio en bóveda abierta
/// (SI <c>BovedaCuenta</c> + neto <c>BovedaMov</c>; misma fórmula que <c>usp_TransferirBoveda</c>).
/// </summary>
public sealed record BovedaSaldoMedioDto(
    int BovedaId,
    int OficinaId,
    short TipoPagoId,
    decimal Disponible);
