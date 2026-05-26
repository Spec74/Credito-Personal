namespace Credito.Modern.Application.CreditoTasas;

public sealed record CalcularTemRequest(decimal Tea, string? FormaPago);

public sealed record CalcularTemResponse(decimal? Tem);
