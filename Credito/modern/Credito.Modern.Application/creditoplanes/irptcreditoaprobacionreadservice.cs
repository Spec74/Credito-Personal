namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCreditoAprobacionReadService
{
    Task<IReadOnlyList<RptCreditoAprobacionRowDto>> ListarAsync(
        DateTime fechaAprobacion,
        int? usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}
