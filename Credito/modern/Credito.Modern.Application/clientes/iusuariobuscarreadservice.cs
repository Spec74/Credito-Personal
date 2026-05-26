namespace Credito.Modern.Application.Clientes;

/// <summary>Paridad <c>ClienteController.BuscarUsuario</c> (personaId + etiqueta).</summary>
public interface IUsuarioBuscarReadService
{
    Task<IReadOnlyList<ClienteBuscarItemDto>> BuscarAsync(
        string term,
        CancellationToken cancellationToken = default);
}
