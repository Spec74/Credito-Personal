namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Resultado equivalente a <c>CreditoController.ValidarCierreCajaDiario</c> (mensajes vacíos = puede cerrar).</summary>
public sealed record ValidarCierreCajaDiarioResponse(bool PuedeCerrar, IReadOnlyList<string> Blockers);
