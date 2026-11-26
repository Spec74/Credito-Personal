namespace Credito.Modern.Application.MediosPago;

public interface IOficinaMedioPagoService
{
    Task<IReadOnlyList<OficinaMedioPagoDto>> ListByOficinaAsync(
        int oficinaId,
        bool incluirInactivos,
        bool soloRaices,
        CancellationToken cancellationToken);

    Task<OficinaMedioPagoOperacionResponse> GuardarAsync(
        GuardarOficinaMedioPagoRequest request,
        CancellationToken cancellationToken);

    Task<OficinaMedioPagoOperacionResponse> SetActivoAsync(
        int oficinaMedioPagoId,
        int oficinaId,
        bool activo,
        CancellationToken cancellationToken);
}
