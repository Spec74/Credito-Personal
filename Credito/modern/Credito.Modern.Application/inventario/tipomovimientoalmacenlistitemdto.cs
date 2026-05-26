namespace Credito.Modern.Application.Inventario;

public sealed record TipoMovimientoAlmacenListItemDto(
    int TipoMovimientoId,
    string Denominacion,
    string? Descripcion,
    bool IndEntrada,
    bool? IndTransferencia,
    bool? IndDevolucion,
    bool Estado);
