namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCreditoCondonadoReadService
{
    Task<IReadOnlyList<RptCreditoCondonadoRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        int oficinaId,
        int? usuarioId,
        CancellationToken cancellationToken = default);
}
