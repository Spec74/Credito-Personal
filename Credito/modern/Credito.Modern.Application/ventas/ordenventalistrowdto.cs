namespace Credito.Modern.Application.Ventas;

public sealed record OrdenVentaListRowDto(
    int OrdenVentaId,
    DateTime FechaReg,
    string Cliente,
    decimal TotalDescuento,
    decimal TotalNeto,
    string TipoVenta,
    string Estado,
    string? EstadoCredito,
    bool PuedeEliminar);

public sealed record OrdenVentaListPageDto(
    IReadOnlyList<OrdenVentaListRowDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
