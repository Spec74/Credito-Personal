namespace Credito.Modern.Application.CreditoTareas;

/// <summary>Fila de listado (paridad <c>TareaBL.ListarTareasUsuario</c>).</summary>
public sealed record TareaListItemDto(
    int TareaId,
    int CreditoId,
    string ClienteDni,
    string ClienteNombre,
    decimal MontoCredito,
    string NombreUsuario,
    DateTime FechaCreacion,
    DateTime? FechaCompletada,
    string Estado,
    int TotalSubtareas,
    int SubtareasCompletadas);
