namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptClientesBloqueadosReadService
{
    Task<List<RptClientesBloqueadosRowDto>> ListarAsync(
        int oficinaId,
        int? usuarioId,
        CancellationToken cancellationToken = default);
}
