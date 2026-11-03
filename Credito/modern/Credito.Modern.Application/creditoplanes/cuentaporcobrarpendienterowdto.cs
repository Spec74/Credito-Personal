namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de CxC pendiente (paridad <c>CxcJgrid</c> / <c>LstCuentasxCobrarJGrid</c>).</summary>
public sealed class CuentaPorCobrarPendienteRowDto
{
    public int OrdenVentaId { get; set; }
    public int CuentaxCobrarId { get; set; }
    public string PersonaCodigo { get; set; } = string.Empty;
    public string PersonaNombre { get; set; } = string.Empty;
    public string Operacion { get; set; } = string.Empty;
    /// <summary>Texto operativo: GAD puro vs GAD + central riesgo (el Monto de CxC es la suma).</summary>
    public string Concepto { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal MontoGastosAdm { get; set; }
    public decimal CentralRiesgo { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaReg { get; set; }
}
