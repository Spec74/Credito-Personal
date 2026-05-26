namespace Credito.Modern.Application.CreditoPlanes;

public interface IVerificarPagoTransferenciaWriteService
{
    /// <summary>Marca <c>IndTransferenciaVerificada</c> en <c>CREDITO.MovimientoCajaExtension</c>.</summary>
    Task<VerificarPagoTransferenciaResponse> VerificarAsync(
        int movimientoCajaId,
        CancellationToken cancellationToken = default);
}
