namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Cuerpo de <c>POST /api/v1/credito/transferir-boveda-caja</c>
/// (paridad <c>BovedaMovBL.TransferirBovedaCaja</c> con embudo banco→destino).
/// </summary>
public sealed record TransferirBovedaCajaRequest(
    int OficinaId,
    int CajaId,
    decimal Importe,
    string Descripcion,
    short TipoPagoOrigenId = 1,
    short TipoPagoDestinoId = 1);

/// <summary>
/// Paridad <c>BovedaController.TransferirAAnalista</c>: resuelve caja abierta del analista
/// y aplica embudo (origen = banco, destino = efectivo).
/// </summary>
public sealed record TransferirBovedaAnalistaRequest(
    int OficinaId,
    int UsuarioAnalistaId,
    short TipoPagoOrigenId,
    decimal Importe,
    string Descripcion);
