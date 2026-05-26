namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Paridad <c>SaldosController.ValidarCierre</c> / <c>ValidarCierreCajaChica</c>
/// (mensaje vacío = puede continuar).
/// </summary>
public sealed record ValidarCierreSaldosResponse(bool PuedeCerrar, string Mensaje);
