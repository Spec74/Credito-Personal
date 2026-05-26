namespace Credito.Modern.Application.CreditoTareas;

public sealed record SubtareaDto(
    int SubtareaId,
    int TareaId,
    string Titulo,
    bool Completada,
    DateTime? FechaCompletada);

public sealed record TareaDetalleDto(
    int TareaId,
    int CreditoId,
    string ClienteDni,
    string ClienteNombre,
    decimal MontoCredito,
    string? NombreUsuario,
    DateTime FechaCreacion,
    DateTime? FechaCompletada,
    string Estado,
    int TotalSubtareas,
    int SubtareasCompletadas,
    IReadOnlyList<SubtareaDto> Subtareas);

public sealed record CreditoTareaBuscarDto(
    int CreditoId,
    int PersonaId,
    string Dni,
    string Nombre,
    decimal MontoCredito,
    string Label);

public sealed record SubtareaGuardarRequest(string Titulo, bool Completada);

public sealed record GuardarTareaRequest(
    int TareaId,
    int CreditoId,
    IReadOnlyList<SubtareaGuardarRequest> Subtareas);

public sealed record GuardarTareaResponse(int TareaId, string Mensaje);

public sealed record TareaOperacionResponse(bool Success, string Mensaje);

public sealed record PuedeEditarTareaResponse(bool PuedeEditar);

public sealed record CompletarTareaRequest(bool Completada);
