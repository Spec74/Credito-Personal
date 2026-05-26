namespace Credito.Modern.Application.Almacenes;

public sealed record AlmacenListItemDto(
    int AlmacenId,
    int? OficinaId,
    string Denominacion,
    string? Descripcion,
    bool? IndEstadoApertura,
    DateTime? FechaApertura,
    bool Estado);
