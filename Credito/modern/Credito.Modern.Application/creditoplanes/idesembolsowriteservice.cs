namespace Credito.Modern.Application.CreditoPlanes;

public interface IDesembolsoWriteService
{
    Task<RealizarDesembolsoResponse> RealizarAsync(
        int cajaDiarioId,
        int creditoId,
        int usuarioId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);
}
