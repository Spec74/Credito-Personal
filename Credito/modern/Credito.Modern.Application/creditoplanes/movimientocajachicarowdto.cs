namespace Credito.Modern.Application.CreditoPlanes;

public sealed record MovimientoCajaChicaRowDto(
    int MovimientoCajaChicaId,
    int CajaChicaDiarioId,
    DateTime FechaReg,
    bool IndEntrada,
    string? Persona,
    string Operacion,
    string? Descripcion,
    decimal ImportePago,
    bool Estado);
