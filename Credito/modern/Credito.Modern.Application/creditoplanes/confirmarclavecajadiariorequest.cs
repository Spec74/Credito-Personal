namespace Credito.Modern.Application.CreditoPlanes;

public sealed record ConfirmarClaveCajaDiarioRequest(string Clave);

public sealed record ConfirmarClaveCajaDiarioResponse(bool Autorizado, string? Mensaje);
