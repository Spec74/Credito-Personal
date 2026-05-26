namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Paridad <c>CreditoController.ValidarDesembolso</c>.</summary>
public sealed record ValidarDesembolsoResponse(bool PuedeDesembolsar, string? Mensaje);
