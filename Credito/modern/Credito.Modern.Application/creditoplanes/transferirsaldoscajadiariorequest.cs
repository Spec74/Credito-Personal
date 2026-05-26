namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Paridad <c>CajaDiarioController.TransferirSaldos</c>.
/// <c>cajaIdDestino</c> null = transferir a bóveda de la oficina; con valor = otra caja abierta.
/// </summary>
public sealed record TransferirSaldosCajaDiarioRequest(
    int OficinaId,
    int CajaDiarioId,
    decimal Importe,
    string Descripcion,
    int? CajaIdDestino);
