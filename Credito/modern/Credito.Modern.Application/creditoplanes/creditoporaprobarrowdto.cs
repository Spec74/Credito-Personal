namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de créditos en estado PEN (paridad <c>CreditoBL.LstCreditoAprobarJGrid</c>).</summary>
public sealed record CreditoPorAprobarRowDto(
    int CreditoId,
    int PersonaId,
    string? Codigo,
    string? Cliente,
    string? Documento,
    decimal Monto,
    decimal Interes,
    string Estado,
    string? Agente);

public sealed record CreditosPorAprobarListResponse(
    IReadOnlyList<CreditoPorAprobarRowDto> Items,
    int Total);
