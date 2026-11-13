namespace Credito.Modern.Application.Clientes;

/// <summary>Autocomplete usuarios activos (bóveda temporal, etc.).</summary>
public interface IUsuarioBuscarReadService
{
    Task<IReadOnlyList<UsuarioBuscarItemDto>> BuscarAsync(
        string term,
        CancellationToken cancellationToken = default);
}
