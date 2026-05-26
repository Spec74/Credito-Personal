namespace Credito.Modern.Application.Clientes;

public interface IClienteDetalleReadService
{
    Task<ClienteDetalleDto?> ObtenerPorPersonaIdAsync(
        int personaId,
        CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>ValidarClienteDNI</c>: true si ya hay fila en MAESTRO.Cliente con ese documento.</summary>
    /// <summary>Paridad <c>ValidarClienteDNI</c>: true si ya hay fila en MAESTRO.Cliente con ese documento.</summary>
    Task<bool> ExisteDocumentoAsync(
        string numeroDocumento,
        int? excluirPersonaId,
        CancellationToken cancellationToken = default);

    Task<PersonaPorDocumentoDto?> ObtenerPersonaPorDocumentoAsync(
        string numeroDocumento,
        CancellationToken cancellationToken = default);
}
