namespace Credito.Modern.Application.Almacenes;

public interface IMovimientoEntradaReadService
{
    Task<MovimientoEntradaListPageDto> ListarEntradasAsync(
        int oficinaId,
        int almacenId,
        string? buscar,
        int articuloId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<MovimientoEntradaDetalleResponse?> ObtenerEntradaAsync(
        int movimientoId,
        CancellationToken cancellationToken = default);

    Task<bool> AlmacenPerteneceOficinaAsync(
        int almacenId,
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<bool> TipoMovimientoEsEntradaAsync(
        int tipoMovimientoId,
        CancellationToken cancellationToken = default);
}
