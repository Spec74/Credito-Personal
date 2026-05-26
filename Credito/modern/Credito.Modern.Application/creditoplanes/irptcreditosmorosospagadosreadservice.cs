namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCreditosMorososPagadosReadService
{
    Task<IReadOnlyList<RptCreditosMorososPagadosRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        int? usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}
