namespace Credito.Modern.Application.ValorTablas;

public sealed record ValorTablaListItemDto(
    int TablaId,
    int ItemId,
    string Denominacion,
    string? DesCorta,
    string? Valor);
