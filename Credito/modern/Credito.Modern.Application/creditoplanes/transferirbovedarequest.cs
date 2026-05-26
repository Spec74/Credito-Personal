namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Cuerpo de <c>POST /api/v1/credito/transferir-boveda</c> (paridad <c>usp_TransferirBoveda</c> /
/// <c>BovedaMovBL.TransferiraOficina</c> / aceptación temporal).
/// </summary>
public sealed record TransferirBovedaRequest(
    int OficinaId,
    int BovedaInicioId = 0,
    int BovedaDestinoId = 0,
    string Glosa = "",
    decimal Monto = 0m,
    int FlagAceptar = 0,
    int BovedaMovTempId = 0);
