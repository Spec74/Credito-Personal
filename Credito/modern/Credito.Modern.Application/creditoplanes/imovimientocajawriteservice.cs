namespace Credito.Modern.Application.CreditoPlanes;

public interface IMovimientoCajaWriteService
{
    Task<AnularMovimientoCajaResponse> AnularAsync(
        int movimientoCajaId,
        string observacion,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
