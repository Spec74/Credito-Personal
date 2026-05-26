namespace Credito.Modern.Application.CreditoPlanes;

public interface ICreditoSolicitudWriteService
{
    Task<CrearSolicitudCreditoResponse> CrearSolicitudAsync(
        int oficinaId,
        int personaId,
        int usuarioId,
        DateTime fechaReg,
        CancellationToken cancellationToken = default);
}
