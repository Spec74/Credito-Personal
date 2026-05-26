namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCreditosActivosReadService
{
    Task<IReadOnlyList<RptCreditosActivosRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        int? usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}
