namespace Credito.Modern.Application.CreditoPlanes;

public interface IBovedaListadoReadService
{
    Task<(IReadOnlyList<BovedaListadoRowDto> Rows, int Total)> ListarAsync(
        int oficinaId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
