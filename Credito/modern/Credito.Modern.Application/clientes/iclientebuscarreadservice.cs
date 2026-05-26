namespace Credito.Modern.Application.Clientes;

public interface IClienteBuscarReadService
{
    Task<IReadOnlyList<ClienteBuscarItemDto>> BuscarAsync(
        string term,
        CancellationToken cancellationToken = default);
}
