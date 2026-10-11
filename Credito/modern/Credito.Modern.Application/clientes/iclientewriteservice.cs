namespace Credito.Modern.Application.Clientes;

public interface IClienteWriteService
{
    Task<GuardarClienteResponse> GuardarAsync(
        GuardarClienteRequest request,
        int usuarioRegId,
        DateTime fechaRegistro,
        CancellationToken cancellationToken = default);

    Task<bool> ToggleActivoAsync(int personaId, CancellationToken cancellationToken = default);

    Task<bool> ToggleBloqueadoAsync(int personaId, CancellationToken cancellationToken = default);

    Task<CrearPersonaRapidaResponse> CrearPersonaRapidaAsync(
        CrearPersonaRapidaRequest request,
        int usuarioRegId,
        DateTime fechaRegistro,
        CancellationToken cancellationToken = default);

    Task<bool> HabilitarDepuradoAsync(int personaId, CancellationToken cancellationToken = default);

    /// <summary>Actualiza <c>MAESTRO.Cliente.Latitud/Longitud</c> sin tocar el resto de la ficha.</summary>
    Task ActualizarUbicacionAsync(
        int personaId,
        ActualizarUbicacionClienteRequest request,
        CancellationToken cancellationToken = default);
}
