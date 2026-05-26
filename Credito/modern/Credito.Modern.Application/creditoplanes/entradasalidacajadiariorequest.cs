namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/entrada-salida-caja-diario</c>.</summary>
public sealed record EntradaSalidaCajaDiarioRequest(
    int OficinaId,
    int CajaDiarioId,
    int PersonaId,
    int TipoOperacionId,
    decimal Importe,
    string? Descripcion,
    int TipoPagoId);
