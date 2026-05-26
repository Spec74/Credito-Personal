namespace Credito.Modern.Application.CreditoPlanes;

public interface ICreditosPorPersonaReadService
{
    Task<IReadOnlyList<CreditoPorPersonaRowDto>> ListarDesembolsadosPorPersonaAsync(
        int personaId,
        int usuarioId,
        bool esCajaCentral,
        CancellationToken cancellationToken = default);
}
