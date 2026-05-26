namespace Credito.Modern.Application.Ocupaciones;

public interface IOcupacionReadService
{
    /// <summary>Ocupaciones activas (<c>Estado = 1</c>), alineado al combo de cliente en MVC.</summary>
    Task<List<OcupacionListItemDto>> GetActivasAsync(CancellationToken cancellationToken = default);
}
