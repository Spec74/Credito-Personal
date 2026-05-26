namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCreditosCierresReadService
{
    Task<IReadOnlyList<RptCreditosCierresRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        int? usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}
