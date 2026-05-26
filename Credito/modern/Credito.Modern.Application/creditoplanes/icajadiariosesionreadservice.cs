namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaDiarioSesionReadService
{
    Task<CajaDiarioSesionDto?> ObtenerSesionAbiertaAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}
