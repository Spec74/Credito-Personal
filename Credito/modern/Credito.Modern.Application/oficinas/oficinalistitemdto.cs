namespace Credito.Modern.Application.Oficinas;

/// <summary>Oficina activa para listados (equivalente a combo del legado).</summary>
public sealed class OficinaListItemDto
{
    public int OficinaId { get; set; }
    public string? Denominacion { get; set; }
    public bool IndPrincipal { get; set; }
    public bool Estado { get; set; }
}
