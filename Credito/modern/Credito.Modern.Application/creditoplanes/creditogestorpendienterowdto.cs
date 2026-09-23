namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Paridad <c>CajaDiarioBL.LstCreditoPendienteJGrid</c> (créditos DES del gestor).</summary>
public sealed class CreditoGestorPendienteRowDto
{
    public int CreditoId { get; set; }
    public string PersonaCodigo { get; set; } = string.Empty;
    public string PersonaNombre { get; set; } = string.Empty;
    public decimal MontoCredito { get; set; }
    public int PersonaId { get; set; }
    /// <summary>
    /// Paridad <c>CreditoPendienteJGrid.DeudaPendiente</c>: capital pendiente + mora total (no CAN).
    /// Calculado set-based en SQL (optimización vs loop EF en BL legado).
    /// </summary>
    public decimal DeudaPendiente { get; set; }
}

public sealed record TieneCxcPendienteResponse(bool TienePendientes);
