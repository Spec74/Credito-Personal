namespace Credito.Modern.Application.CreditoPlanes;

public sealed record SolicitarCondonacionRequest(
    int OficinaId,
    int CajaDiarioId,
    int CreditoId,
    decimal MoraCondonacion);

public sealed record SolicitarCondonacionResponse(bool Success, string? Mensaje);

public sealed record CondonacionPendienteDto(
    int Id,
    int CreditoId,
    int PersonaId,
    string NombreCliente,
    string NombreUsuario,
    decimal MontoCredito,
    decimal MoraCondonacion,
    decimal TotalPago,
    DateTime Fecha,
    int CajaDiarioId);

public sealed record CondonacionPendienteCreditoDto(
    bool TienePendiente,
    decimal MoraCondonacion,
    decimal TotalPago,
    int? Id);
