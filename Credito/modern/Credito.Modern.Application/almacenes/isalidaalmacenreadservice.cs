namespace Credito.Modern.Application.Almacenes;

public interface ISalidaAlmacenReadService
{
    Task<BuscarSerieSalidaResponse> BuscarSerieAsync(
        string numeroSerie,
        CancellationToken cancellationToken = default);

    Task<int?> GetAlmacenPredeterminadoOficinaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<bool> TipoMovimientoEsSalidaAsync(
        int tipoMovimientoId,
        CancellationToken cancellationToken = default);
}
