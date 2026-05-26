namespace Credito.Modern.Application.Ventas;

public interface IOrdenVentaEnvioReadService
{
    Task<OrdenVentaEnvioDto?> GetOrdenAsync(int ordenVentaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListarDescripcionesDetalleActivasAsync(
        int ordenVentaId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteCreditoVinculadoAsync(int ordenVentaId, CancellationToken cancellationToken = default);
}
