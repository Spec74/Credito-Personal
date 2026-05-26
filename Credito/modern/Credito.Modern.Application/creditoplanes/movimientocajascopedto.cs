namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Contexto de un movimiento de caja para validaciones de escritura.</summary>
public sealed record MovimientoCajaScopeDto(
    int OficinaId,
    int CajaDiarioId,
    string Operacion,
    bool EstadoActivo,
    bool CajaDiarioCerrada);
