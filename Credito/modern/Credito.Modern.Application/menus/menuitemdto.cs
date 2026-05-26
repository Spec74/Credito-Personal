namespace Credito.Modern.Application.Menus;

/// <summary>
/// Fila devuelta por <c>MAESTRO.usp_MenuLst</c> (equivalente a <c>usp_MenuLst_Result</c> del legado).
/// </summary>
public sealed class MenuItemDto
{
    public int MenuId { get; set; }
    public string? Denominacion { get; set; }
    public string? Modulo { get; set; }
    public string? Url { get; set; }
    public string? Icono { get; set; }
    public bool? IndPadre { get; set; }
    public decimal? Orden { get; set; }
    public decimal? Referencia { get; set; }
}
