namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Paridad <c>CajaDiarioBL.LstCreditoPendienteJGrid</c> (créditos DES del gestor).</summary>
public sealed class CreditoGestorPendienteRowDto
{
    public int CreditoId { get; set; }
    public string PersonaCodigo { get; set; } = string.Empty;
    public string PersonaNombre { get; set; } = string.Empty;
    public decimal MontoCredito { get; set; }
    public int PersonaId { get; set; }
}

public sealed record TieneCxcPendienteResponse(bool TienePendientes);
