namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de créditos aprobados pendientes de desembolso (paridad <c>LstDesembolsoJGrid</c>).</summary>
public sealed class DesembolsoPendienteRowDto
{
    public int CreditoId { get; set; }
    public string PersonaCodigo { get; set; } = string.Empty;
    public string PersonaNombre { get; set; } = string.Empty;
    public decimal MontoCredito { get; set; }
    public decimal MontoGastosAdm { get; set; }
    public decimal MontoDesembolso { get; set; }
    public string Estado { get; set; } = string.Empty;
}
