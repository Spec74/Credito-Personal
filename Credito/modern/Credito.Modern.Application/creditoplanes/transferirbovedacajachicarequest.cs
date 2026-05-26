namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/transferir-boveda-caja-chica</c>.</summary>
public sealed record TransferirBovedaCajaChicaRequest(int OficinaId, decimal Importe, string Descripcion);
