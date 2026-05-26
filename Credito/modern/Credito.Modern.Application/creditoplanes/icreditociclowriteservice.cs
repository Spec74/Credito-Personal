namespace Credito.Modern.Application.CreditoPlanes;

public interface ICreditoCicloWriteService
{
    Task<CreditoCicloOperacionResponse> AprobarAsync(
        int creditoId,
        int opcion,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<CreditoCicloOperacionResponse> AnularAsync(
        int creditoId,
        string observacion,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<CreditoCicloOperacionResponse> ReprogramarAsync(
        int creditoId,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<CreditoCicloOperacionResponse> ProrrogarAsync(
        int creditoId,
        int dias,
        CancellationToken cancellationToken = default);

    Task<CrearCreditoResponse> CrearDesdeSolicitudAsync(
        int solicitudCreditoId,
        int productoId,
        string tipoCuota,
        decimal montoInicial,
        decimal montoCredito,
        decimal montoGastosAdm,
        string indGastosAdm,
        string formaPago,
        int nroCuotas,
        decimal interes,
        DateTime fechaPrimerPago,
        string observacion,
        int usuarioId,
        bool indCentralRiesgo,
        CancellationToken cancellationToken = default);

    Task<CreditoCicloOperacionResponse> RechazarAsync(
        int creditoId,
        int? ordenVentaId,
        CancellationToken cancellationToken = default);
}

