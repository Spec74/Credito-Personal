namespace Credito.Modern.Application.Clientes;

public interface IClienteAuxReadService
{
    Task<IReadOnlyList<ClienteBuscarItemDto>> BuscarDistritosAsync(
        string term,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClienteBuscarItemDto>> BuscarPersonasAsync(
        string term,
        CancellationToken cancellationToken = default);
}
