namespace Credito.Modern.Application.CreditoPlanes;

public interface IDesembolsoReadService
{
    Task<IReadOnlyList<DesembolsoPendienteRowDto>> ListarPendientesAsync(
        int oficinaId,
        int usuarioRegId,
        int personaId,
        CancellationToken cancellationToken = default);

    Task<ValidarDesembolsoResponse> ValidarAsync(
        int cajaDiarioId,
        int creditoId,
        CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>CajaDiarioBL.LstCreditoPendienteJGrid</c> (Estado DES, usuario actual).</summary>
    Task<IReadOnlyList<CreditoGestorPendienteRowDto>> ListarCreditosGestorDesembolsadosAsync(
        int usuarioRegId,
        CancellationToken cancellationToken = default);
}
