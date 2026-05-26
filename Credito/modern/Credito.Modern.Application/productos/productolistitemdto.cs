namespace Credito.Modern.Application.Productos;

public sealed record ProductoListItemDto(
    int ProductoId,
    string Denominacion,
    decimal InteresMinima,
    decimal InteresMaxima,
    int DiasGracia,
    decimal ImporteMoratorio,
    bool Estado,
    bool IndMora);
