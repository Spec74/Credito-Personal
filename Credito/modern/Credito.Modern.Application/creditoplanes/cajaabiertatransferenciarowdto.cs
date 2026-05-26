namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Combo de cajas abiertas (paridad <c>CajaBL.ListarCajasAbiertas</c>).</summary>
public sealed class CajaAbiertaTransferenciaRowDto
{
    public int CajaId { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
}
