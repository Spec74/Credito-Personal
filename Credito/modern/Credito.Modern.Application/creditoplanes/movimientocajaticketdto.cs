namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Tramo de pago mixto en el ticket.</summary>
public sealed record MovimientoCajaTicketMedioDto(
    int MovimientoCajaId,
    string Nombre,
    decimal Importe);

/// <summary>Datos unificados para ticket PDF (caja, bóveda, caja chica).</summary>
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
    decimal? CreditoTotal,
    /// <summary>Medio tipado (Yape Gerente, Interbank René, Efectivo…).</summary>
    string? MedioPago = null,
    /// <summary>Título del comprobante; default según dominio.</summary>
    string? TituloComprobante = null,
    decimal? Entrada = null,
    decimal? Salida = null,
    /// <summary>Si hay 2+ tramos, el PDF lista cada medio y el total cobrado.</summary>
    IReadOnlyList<MovimientoCajaTicketMedioDto>? MediosPago = null);

public enum MovimientoCajaTicketLayout
{
    Simple,
    CuotaCredito,
    CuotaLibre,
}
