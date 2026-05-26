namespace Credito.Modern.Application.CreditoPlanes;

public sealed record CreditoPorPersonaRowDto(
    int CreditoId,
    string Descripcion,
    decimal MontoCredito);
