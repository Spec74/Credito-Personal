namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptSaldosCajaReadService
{
    Task<List<RptSaldosCajaRowDto>> ListarAsync(
        int cajaDiarioId,
        bool indCajaChica,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Paridad <c>LstMovimientosCajaJGrid</c>: incluye anulados. PDF/CSV siguen usando <see cref="ListarAsync"/>.
    /// </summary>
    Task<List<RptSaldosCajaRowDto>> ListarArqueoAsync(
        int cajaDiarioId,
        bool incluirAnulados,
        CancellationToken cancellationToken = default);
}
