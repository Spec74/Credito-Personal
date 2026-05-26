namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaChicaRendicionWriteService
{
    Task<string> CrearAsync(
        CrearRendicionCajaChicaRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int rendicionId, CancellationToken cancellationToken = default);

    Task<(string? Error, CerrarRendicionCajaChicaResponse? Result)> CerrarAsync(
        int movimientoCajaChicaId,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
