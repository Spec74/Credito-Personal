namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCreditoRentabilidadReadService
{
    Task<IReadOnlyList<RptCreditoRentabilidadRowDto>> ListarAsync(
        int oficinaId,
        DateTime fechaIni,
        DateTime fechaFin,
        string estadoCredito,
        CancellationToken cancellationToken = default);
}
