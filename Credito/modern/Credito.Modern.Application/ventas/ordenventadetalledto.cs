namespace Credito.Modern.Application.Ventas;

public sealed record OrdenVentaCabeceraDto(
    int OrdenVentaId,
    int OficinaId,
    int PersonaId,
    string Cliente,
    decimal Subtotal,
    decimal TotalImpuesto,
    decimal TotalNeto,
    decimal TotalDescuento,
    string Estado,
    string TipoVenta,
    DateTime FechaReg,
    string? EstadoCredito,
    bool PuedeEliminar);

public sealed record OrdenVentaDetLineaDto(
    int OrdenVentaDetId,
    int ArticuloId,
    int Cantidad,
    string Descripcion,
    decimal ValorVenta,
    decimal Descuento,
    decimal Subtotal,
    bool Estado);

public sealed record OrdenVentaDetalleResponse(
    OrdenVentaCabeceraDto Cabecera,
    IReadOnlyList<OrdenVentaDetLineaDto> Detalle,
    int CantidadTotal);
