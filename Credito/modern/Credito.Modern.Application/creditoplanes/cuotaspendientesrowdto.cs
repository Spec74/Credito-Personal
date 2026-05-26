namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Filas devueltas por <c>CREDITO.usp_CuotasPendientes</c>.</summary>
public sealed record CuotasPendientesRowDto(
    int? PlanPagoId,
    string? Glosa,
    DateTime? FechaVencimiento,
    decimal? Amortizacion,
    decimal? Interes,
    decimal? GastosAdm,
    decimal? Cuota,
    int? DiasAtrazo,
    decimal? ImporteMora,
    decimal? Descuento,
    decimal? Cargo,
    decimal? PagoLibre,
    decimal? PagoCuota);
