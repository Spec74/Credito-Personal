namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Datos unificados para ticket PDF de movimiento de caja (paridad ReporteMovimientoCaja).</summary>
public sealed record MovimientoCajaTicketDto(
    int MovimientoCajaId,
    MovimientoCajaTicketLayout Layout,
    int PersonaId,
    string Cliente,
    string User,
    DateTime FechaReg,
    string Oficina,
    string Producto,
    string Concepto,
    string Articulo,
    decimal ImportePago,
    int? CreditoId,
    decimal? SaldoAnterior,
    decimal? PagoDeuda,
    decimal? Interes,
    decimal? MoraCargo,
    decimal? Descuento,
    decimal? ImporteLibreAnt,
    decimal? ImporteLibre,
    decimal? ImportePagado,
    decimal? SaldoCapital,
    string? CuotasPagadas,
    string? ProximaCuota,
    int? CuotasAtrazadas,
    decimal? MoraTotalPendiente,
    string? EstadoCredito,
    decimal? CreditoTotal);

public enum MovimientoCajaTicketLayout
{
    Simple,
    CuotaCredito,
    CuotaLibre,
}
