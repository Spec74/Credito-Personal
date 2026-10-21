namespace Credito.Modern.Application.Pla;

public interface IPlaService
{
    Task<PlaParametroDto?> ObtenerParametroAsync(CancellationToken cancellationToken = default);

    Task<PlaParametroDto> GuardarParametroAsync(
        GuardarPlaParametroRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaCandidatoRoDto>> ListarCandidatosRoAsync(
        int oficinaId,
        int anio,
        int mes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaInusualDto>> ListarInusualAsync(
        int oficinaId,
        int anio,
        int mes,
        CancellationToken cancellationToken = default);

    Task<RegistrarPlaInusualResponse> RegistrarInusualAsync(
        RegistrarPlaInusualRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
