namespace Credito.Modern.Application.Prendario;

public interface IPrendarioAvisoEnvioService
{
    Task<PrendarioAvisoEnvioResumenDto> EnviarPendientesAsync(
        int? oficinaId,
        int diasAntes,
        int? creditoId = null,
        CancellationToken cancellationToken = default);
}
