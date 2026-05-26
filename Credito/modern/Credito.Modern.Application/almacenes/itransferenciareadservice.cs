namespace Credito.Modern.Application.Almacenes;

public interface ITransferenciaReadService
{
    Task<TransferenciaListPageDto> ListarAsync(
        int oficinaId,
        string? buscar,
        int almacenId,
        int articuloId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TransferenciaCabeceraDto?> ObtenerCabeceraAsync(
        int transferenciaId,
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransferenciaDetalleLineaDto>> ListarDetalleAsync(
        int transferenciaId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}
