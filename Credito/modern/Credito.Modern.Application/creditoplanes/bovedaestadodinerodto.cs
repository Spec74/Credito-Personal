namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Paridad cabecera <c>Boveda/Index</c> — bloque «Estado de dinero».</summary>
public sealed record BovedaEstadoDineroDto(
    decimal SaldoBoveda,
    decimal MontoCajaChica,
    decimal MontoCajas,
    decimal MontoPlanPagoPendiente,
    decimal CreditoVencido,
    decimal VencidoMenor60,
    decimal VencidoMayor60,
    decimal VencidoIrrecuperable,
    decimal TotalFondo);
