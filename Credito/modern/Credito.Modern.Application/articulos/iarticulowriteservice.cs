using Credito.Modern.Application.Maestros;

namespace Credito.Modern.Application.Articulos;

public interface IArticuloWriteService
{
    Task<MaestroOperacionResponse> GuardarAsync(
        GuardarArticuloRequest request,
        CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> SubirImagenAsync(
        int articuloId,
        Stream fileStream,
        string originalFileName,
        CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> EliminarImagenAsync(
        int articuloId,
        string nombreArchivo,
        CancellationToken cancellationToken = default);
}
