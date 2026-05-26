namespace Credito.Modern.Application.Almacenes;

public interface ITransferenciaWriteService
{
    Task<CrearTransferenciaResponse> CrearAsync(
        int oficinaId,
        int usuarioId,
        int almacenDestinoId,
        DateTime fecha,
        CancellationToken cancellationToken = default);

    Task<ValidarSerieTransferenciaResponse> ValidarYAgregarSerieAsync(
        int oficinaId,
        int transferenciaId,
        string numeroSerie,
        CancellationToken cancellationToken = default);

    Task<bool> EliminarSeriesPorArticuloAsync(
        int oficinaId,
        int transferenciaId,
        int articuloId,
        CancellationToken cancellationToken = default);

    Task<bool> DesconfirmarAsync(
        int oficinaId,
        int transferenciaId,
        CancellationToken cancellationToken = default);

    Task<ConfirmarTransferenciaResponse> ConfirmarAsync(
        int oficinaId,
        int transferenciaId,
        CancellationToken cancellationToken = default);
}
