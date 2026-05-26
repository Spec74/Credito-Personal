namespace Credito.Modern.Application.Ventas;

public interface IOrdenVentaReadService
{
    Task<OrdenVentaListPageDto> ListarAsync(
        int oficinaId,
        bool entregado,
        string? buscar,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OrdenVentaDetalleResponse?> ObtenerDetalleAsync(
        int ordenVentaId,
        CancellationToken cancellationToken = default);
}
