namespace Credito.Modern.Application.Prendario;

public interface IPrendarioAvisoEnvioService
{
    Task<PrendarioAvisoEnvioResumenDto> EnviarPendientesAsync(
        int? oficinaId,
        int diasAntes,
        int? creditoId,
        string origen,
        CancellationToken cancellationToken = default);
}
