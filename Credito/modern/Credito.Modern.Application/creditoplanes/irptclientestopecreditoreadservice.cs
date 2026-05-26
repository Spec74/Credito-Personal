namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptClientesTopeCreditoReadService
{
    Task<List<RptClientesTopeCreditoRowDto>> ListarAsync(
        int oficinaId,
        int? usuarioId,
        CancellationToken cancellationToken = default);
}
