namespace Credito.Modern.Application.CreditoPlanes;

public interface IEntradaSalidaCajaDiarioReadService
{
    Task<bool?> GetTipoOperacionEsEntradaAsync(int tipoOperacionId, CancellationToken cancellationToken = default);

    Task<decimal?> GetSaldoFinalCajaDiarioAsync(int cajaDiarioId, CancellationToken cancellationToken = default);

    Task<bool> CajaDiarioEstaCerradaAsync(int cajaDiarioId, CancellationToken cancellationToken = default);
}
