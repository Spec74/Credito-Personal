namespace Credito.Modern.Application.CreditoPlanes;

public interface IMovimientoCajaTicketReadService
{
    Task<MovimientoCajaTicketDto?> ObtenerAsync(int movimientoCajaId, CancellationToken cancellationToken = default);
}
