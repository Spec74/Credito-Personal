namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptClientesInactivosReadService
{
    Task<IReadOnlyList<RptClientesInactivosRowDto>> ListarAsync(
        DateTime? fechaIni,
        DateTime? fechaFin,
        int? usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}
