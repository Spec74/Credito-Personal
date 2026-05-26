namespace Credito.Modern.Application.Almacenes;

public sealed record CrearMovimientoDetalleRequest(
    int OficinaId,
    int MovimientoId,
    int MovimientoDetId,
    int ArticuloId,
    bool IndAutogenerar,
    string? ListaSerie,
    int Cantidad,
    bool IndCorrelativo,
    decimal PrecioUnitario,
    decimal Descuento,
    int Medida);
