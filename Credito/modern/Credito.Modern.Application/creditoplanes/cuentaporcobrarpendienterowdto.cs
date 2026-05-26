namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de CxC pendiente (paridad <c>CxcJgrid</c> / <c>LstCuentasxCobrarJGrid</c>).</summary>
public sealed class CuentaPorCobrarPendienteRowDto
{
    public int OrdenVentaId { get; set; }
    public int CuentaxCobrarId { get; set; }
    public string PersonaCodigo { get; set; } = string.Empty;
    public string PersonaNombre { get; set; } = string.Empty;
    public string Operacion { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaReg { get; set; }
}
