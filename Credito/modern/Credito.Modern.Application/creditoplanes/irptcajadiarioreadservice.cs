namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCajaDiarioReadService
{
    Task<IReadOnlyList<RptCajaDiarioRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        int? usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}
