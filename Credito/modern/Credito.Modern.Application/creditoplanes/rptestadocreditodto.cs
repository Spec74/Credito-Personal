namespace Credito.Modern.Application.CreditoPlanes;

public sealed record RptEstadoCreditoCabeceraDto(
    int CreditoId,
    string Producto,
    DateTime FechaPrimerPago,
    DateTime FechaVencimiento,
    decimal MontoCredito,
    string Modalidad,
    int NumeroCuotas,
    decimal Interes,
    string Estado,
    string CodigoPersona,
    string Cliente,
    string Analista,
    decimal MontoGastosAdm,
    decimal Total);

public sealed record RptEstadoCreditoInformeDto(
    RptEstadoCreditoCabeceraDto Cabecera,
    IReadOnlyList<EstadoPlanPagoCuotaDto> Cuotas);
