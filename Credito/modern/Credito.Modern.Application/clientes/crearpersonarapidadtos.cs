namespace Credito.Modern.Application.Clientes;

public sealed record CrearPersonaRapidaRequest(
    string Dni,
    string Nombre,
    string ApePaterno,
    string ApeMaterno,
    string? Celular);

public sealed record CrearPersonaRapidaResponse(
    bool Success,
    int PersonaId,
    string Label);
