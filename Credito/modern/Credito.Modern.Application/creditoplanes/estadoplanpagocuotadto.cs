namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Filas devueltas por <c>CREDITO.usp_EstadoPlanPago</c>.</summary>
public sealed record EstadoPlanPagoCuotaDto(
    int PlanPagoId,
    int Numero,
    decimal Capital,
    DateTime FechaVencimiento,
    decimal Amortizacion,
    decimal Interes,
    decimal GastosAdm,
    decimal Cuota,
    string Estado,
    int? DiasAtrazo,
    decimal? ImporteMora,
    decimal? Descuento,
    decimal? Cargo,
    decimal PagoLibre,
    DateTime? FechaPagoCuota,
    decimal? PagoCuota,
    int? MovimientoCajaId);
