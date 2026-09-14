namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Datos para confirmar anulación — paridad <c>CajaDiarioController.ObtenerMovimientoCajaAnular</c>.</summary>
public sealed record MovimientoCajaAnularPreviewDto(
    int MovimientoCajaId,
    int CajaDiarioId,
    int OficinaId,
    string Operacion,
    string? Descripcion,
    string? Persona,
    DateTime FechaReg,
    decimal ImportePago,
    bool PuedeAnular,
    string? Bloqueo);
