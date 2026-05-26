namespace Credito.Modern.Application.CreditoPlanes;

public sealed record RendicionComprobanteRowDto(
    int Id,
    int MovimientoCajaChicaId,
    string? Ruc,
    string? RazonSocial,
    string? DetalleGasto,
    string? TipoDocumento,
    DateTime Fecha,
    string? Serie,
    string? Numero,
    decimal Importe);
