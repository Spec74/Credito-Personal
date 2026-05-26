namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Cuerpo de <c>POST /api/v1/credito/asignar-boveda-temporal</c>.
/// <c>usuarioId &gt; 0</c> = asignar encargado; <c>0</c> = solo transferir a temporal existente.
/// </summary>
public sealed record AsignarBovedaTemporalRequest(
    int OficinaId,
    decimal Importe,
    string Descripcion,
    int UsuarioId = 0);
