namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/transferir-boveda-bancos</c> (paridad <c>BovedaController.RegistrarTransferenciaBancos</c>).</summary>
public sealed record TransferirBovedaBancosRequest(
    int OficinaId,
    short TipoPagoOrigenId,
    short TipoPagoDestinoId,
    decimal Importe,
    string Glosa);

public sealed record TransferirBovedaBancosResponse(bool Success, string Mensaje);
