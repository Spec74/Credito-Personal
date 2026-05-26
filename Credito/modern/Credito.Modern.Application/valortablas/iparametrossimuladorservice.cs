namespace Credito.Modern.Application.ValorTablas;

public interface IParametrosSimuladorService
{
    Task<ParametrosSimuladorDto> ObtenerAsync(CancellationToken cancellationToken = default);

    Task<bool> ActualizarAsync(
        ActualizarParametrosSimuladorRequest request,
        CancellationToken cancellationToken = default);
}
