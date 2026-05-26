namespace Credito.Modern.Application.CreditoPlanes;

public interface IMovimientoBovedaTicketReadService
{
    Task<MovimientoCajaTicketDto?> ObtenerAsync(int movimientoBovedaId, CancellationToken cancellationToken = default);
}
