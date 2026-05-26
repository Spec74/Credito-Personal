namespace Credito.Modern.Application.Ventas;

public interface IOrdenVentaEnvioWriteService
{
    Task<EnviarOrdenVentaResponse> EnviarContadoAsync(
        int ordenVentaId,
        int usuarioId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);

    Task<EnviarOrdenVentaResponse> EnviarCreditoAsync(
        int ordenVentaId,
        int oficinaId,
        int personaId,
        decimal totalNeto,
        IReadOnlyList<string> descripcionesDetalle,
        int usuarioId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);
}
