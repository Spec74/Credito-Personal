namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/transferir-boveda-caja</c> (paridad <c>BovedaController.TransferirCaja</c>).</summary>
public sealed record TransferirBovedaCajaRequest(
    int OficinaId,
    int CajaId,
    decimal Importe,
    string Descripcion);
