namespace Credito.Modern.Application.CreditoPlanes;

public interface ICreditoGestionWriteService
{
    Task<CreditoGestionOperacionResponse> CondonarAsync(
        CondonarCreditoRequest request,
        int usuarioId,
        DateTime fechaServidor,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> ObservarAsync(
        ObservarCreditoRequest request,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> GuardarCargoAsync(
        GuardarCargoCreditoRequest request,
        int usuarioId,
        DateTime fechaServidor,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> SubirEvidenciaAsync(
        int oficinaId,
        int creditoId,
        string extension,
        Stream contenido,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> EliminarEvidenciaAsync(
        int oficinaId,
        int creditoImagenId,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> CambiarAnalistaAsync(
        CambiarAnalistaCreditoRequest request,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> ActualizarTopeAsync(
        ActualizarTopeCreditoRequest request,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> DepurarPersonaAsync(
        DepurarPersonaCreditoRequest request,
        int usuarioId,
        DateTime fechaServidor,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> ActualizarIrrecuperableAsync(
        ActualizarIrrecuperableRequest request,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> ModificarTramiteAdmAsync(
        ModificarTramiteAdmCreditoRequest request,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> ModificarCentralRiesgoAsync(
        ModificarCentralRiesgoCreditoRequest request,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> ActualizarDescuentoPlanPagoAsync(
        ActualizarDescuentoPlanPagoRequest request,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> ActualizarAvalAsync(
        ActualizarAvalCreditoRequest request,
        CancellationToken cancellationToken = default);

    Task<CreditoGestionOperacionResponse> GuardarPrendaAsync(
        GuardarCreditoPrendaRequest request,
        int usuarioId,
        DateTime fechaServidor,
        CancellationToken cancellationToken = default);
}
