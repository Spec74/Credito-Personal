namespace Credito.Modern.Application.CreditoPlanes;

public sealed record RendicionPendienteRowDto(
    int MovimientoCajaChicaId,
    string Cliente,
    string? Descripcion,
    DateTime FechaReg,
    decimal Importe,
    decimal ImporteRendido);
