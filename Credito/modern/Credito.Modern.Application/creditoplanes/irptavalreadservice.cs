namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptAvalReadService
{
    Task<List<RptAvalRowDto>> ListarPorPersonaAsync(
        int personaId,
        CancellationToken cancellationToken = default);
}
