namespace Credito.Modern.Application.CreditoPlanes;

public sealed record GenerarRutaCobrosRequest(int[] CreditoIds);

public sealed record GenerarRutaCobrosResponse(bool Exito, string? UrlCortita, string? Mensaje);
