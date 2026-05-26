namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptSaldosCajaReadService
{
    Task<List<RptSaldosCajaRowDto>> ListarAsync(
        int cajaDiarioId,
        bool indCajaChica,
        CancellationToken cancellationToken = default);
}
