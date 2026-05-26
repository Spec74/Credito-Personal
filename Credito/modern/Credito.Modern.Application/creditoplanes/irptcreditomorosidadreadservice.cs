namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCreditoMorosidadReadService
{
    Task<IReadOnlyList<RptCreditoMorosidadRowDto>> ListarAsync(
        int oficinaId,
        DateTime hastaFecha,
        int diasAtrazoIni,
        int diasAtrazoFin,
        CancellationToken cancellationToken = default);
}
