namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/entrada-salida-caja-chica-diario</c>.</summary>
public sealed record EntradaSalidaCajaChicaDiarioRequest(
    int OficinaId,
    int PersonaId,
    int TipoOperacionId,
    decimal Importe,
    string Descripcion);
