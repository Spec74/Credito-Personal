namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCreditoReadService
{
    Task<IReadOnlyList<RptCreditoRowDto>> ListarAsync(
        int oficinaId,
        int? gestorId,
        string estadoCredito,
        DateTime fechaIni,
        DateTime fechaFin,
        CancellationToken cancellationToken = default);
}
