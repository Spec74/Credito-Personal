namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCobroDiarioReadService
{
    Task<List<RptCobroDiarioRowDto>> ListarAsync(
        int? usuarioId,
        int? oficinaId,
        CancellationToken cancellationToken = default);
}
