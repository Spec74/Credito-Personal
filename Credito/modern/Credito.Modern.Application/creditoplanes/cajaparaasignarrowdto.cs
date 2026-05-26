namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila para combo de cajas disponibles (paridad <c>SaldosController.ObtnerListaCajasCombo</c>).</summary>
public sealed class CajaParaAsignarRowDto
{
    public int CajaId { get; init; }
    public string Denominacion { get; init; } = string.Empty;
}
