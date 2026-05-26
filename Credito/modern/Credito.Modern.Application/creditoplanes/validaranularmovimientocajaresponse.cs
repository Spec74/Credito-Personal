namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Paridad <c>CreditoController.ValidarAnularMovimientoCaja</c>:
/// <c>requiereConfirmacion</c> true cuando operación INI y hay cuotas PAG en el crédito vinculado.
/// </summary>
public sealed record ValidarAnularMovimientoCajaResponse(bool RequiereConfirmacion);
