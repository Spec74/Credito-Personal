namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptClientesNuevosMesReadService
{
    Task<IReadOnlyList<RptCreditoObservadoRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        int oficinaId,
        int? usuarioId,
        CancellationToken cancellationToken = default);
}
