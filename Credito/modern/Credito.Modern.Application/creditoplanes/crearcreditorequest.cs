namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Cuerpo de <c>POST /api/v1/credito/crear-credito</c> (paridad <c>GenerarCredito</c>).
/// <c>solicitudCreditoId</c> sustituye la sesión MVC <c>SolicitudCreditoId</c>.
/// </summary>
public sealed record CrearCreditoPrendaRequest(
    string Descripcion,
    decimal MontoTasacion,
    DateTime FechaRemate,
    string? Observacion,
    string? Marca = null,
    string? Modelo = null,
    string? Serie = null,
    string? Color = null,
    string? CodigoInterno = null);

public sealed record CrearCreditoRequest(
    int OficinaId,
    int SolicitudCreditoId,
    int ProductoId,
    string TipoCuota,
    decimal MontoInicial,
    decimal MontoGastosAdm,
    string IndGastosAdm,
    decimal MontoCredito,
    string Modalidad,
    int NumeroCuotas,
    decimal InteresMensual,
    DateTime FechaPrimerPago,
    string? Observacion,
    bool IndCentralRiesgo,
    CrearCreditoPrendaRequest? Prenda = null);
