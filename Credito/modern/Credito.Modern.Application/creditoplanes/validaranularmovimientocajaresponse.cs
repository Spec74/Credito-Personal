namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Paridad <c>CreditoController.ValidarAnularMovimientoCaja</c> + UI CajaDiario:
/// <c>true</c> = INI con cuotas PAG → diálogo bloqueante
/// («Tiene Pagos de cuotas, No se Puede Anular el Crédito»);
/// <c>false</c> = se puede anular con observación obligatoria.
/// </summary>
public sealed record ValidarAnularMovimientoCajaResponse(bool BloqueadoPorPagosCuota);
