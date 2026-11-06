namespace Credito.Modern.Application.CreditoPlanes;

public sealed record RptEstadoCreditoCabeceraDto(
    int CreditoId,
    int PersonaId,
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
    /// <summary>Usuario que registró el movimiento DES (caja). Vacío si aún no desembolsó.</summary>
    string UsuarioDesembolso,
    decimal MontoGastosAdm,
    decimal CentralRiesgo,
    string TipoGastoAdm,
    decimal Total,
    decimal MontoDesembolso);

public sealed record RptEstadoCreditoInformeDto(
    RptEstadoCreditoCabeceraDto Cabecera,
    IReadOnlyList<EstadoPlanPagoCuotaDto> Cuotas);
