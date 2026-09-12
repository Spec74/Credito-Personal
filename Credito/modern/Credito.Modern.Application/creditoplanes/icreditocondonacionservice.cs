namespace Credito.Modern.Application.CreditoPlanes;

public interface ICreditoCondonacionService
{
    Task<SolicitarCondonacionResponse> SolicitarAsync(
        SolicitarCondonacionRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CondonacionPendienteDto>> ListarPendientesAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<CondonacionPendienteCreditoDto> ObtenerPendientePorCreditoAsync(
        int oficinaId,
        int creditoId,
        CancellationToken cancellationToken = default);

    Task EliminarAsync(
        int oficinaId,
        int id,
        CancellationToken cancellationToken = default);
}
