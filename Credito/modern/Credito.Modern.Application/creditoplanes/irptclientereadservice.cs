namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptClienteReadService
{
    /// <summary>Paridad <c>ReporteController.ReporteCliente</c>.</summary>
    Task<RptClienteInformeDto?> ObtenerAsync(
        int personaId,
        CancellationToken cancellationToken = default);
}
