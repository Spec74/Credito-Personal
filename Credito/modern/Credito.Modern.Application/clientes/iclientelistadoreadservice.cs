namespace Credito.Modern.Application.Clientes;

public interface IClienteListadoReadService
{
    /// <summary>
    /// Listado paginado: sin búsqueda = clientes de créditos del usuario;
    /// con búsqueda = todos los clientes que coincidan (paridad <c>LstClienteJGrid</c>).
    /// </summary>
    Task<ClienteListadoResultDto> ListarAsync(
        int usuarioId,
        string? buscar,
        int page,
        int pageSize,
        string sortField,
        string sortDirection,
        CancellationToken cancellationToken = default);
}
