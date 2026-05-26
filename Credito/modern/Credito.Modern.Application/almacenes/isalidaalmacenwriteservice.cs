namespace Credito.Modern.Application.Almacenes;

public interface ISalidaAlmacenWriteService
{
    Task<RealizarSalidaResponse> RealizarSalidaAsync(
        int oficinaId,
        int almacenId,
        int tipoMovimientoId,
        string glosa,
        IReadOnlyList<SerieSalidaLineaRequest> series,
        DateTime fecha,
        CancellationToken cancellationToken = default);
}
