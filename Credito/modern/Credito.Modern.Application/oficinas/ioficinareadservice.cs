using Credito.Modern.Application.Maestros;

namespace Credito.Modern.Application.Oficinas;

public interface IOficinaReadService
{
    /// <summary>Oficinas con <c>Estado = 1</c> en <c>MAESTRO.Oficina</c>, orden por denominación.</summary>
    Task<List<OficinaListItemDto>> GetActivasAsync(CancellationToken cancellationToken = default);

    Task<List<OficinaAdminListItemDto>> ListGestionAsync(
        bool incluirInactivos,
        string? buscar = null,
        CancellationToken cancellationToken = default);
}