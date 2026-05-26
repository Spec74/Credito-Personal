namespace Credito.Modern.Application.Almacenes;

public sealed record ActualizarMovimientoRequest(
    int OficinaId,
    int MovimientoId,
    int TipoMovimientoId,
    DateTime Fecha,
    string? Observacion);
