namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo JSON para simular plan de pagos (paridad con <c>CreditoBL.SimuladorCredito</c>).</summary>
public sealed record SimuladorCreditoRequest(
    decimal Monto,
    string? FormaPago,
    int NroCuotas,
    decimal InteresMensual,
    DateTime FechaPrimerPago,
    decimal? GastosAdm = null);
