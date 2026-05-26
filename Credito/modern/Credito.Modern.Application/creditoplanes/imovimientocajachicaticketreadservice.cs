namespace Credito.Modern.Application.CreditoPlanes;

public interface IMovimientoCajaChicaTicketReadService
{
    /// <summary>Ticket del movimiento si pertenece a la sesión abierta del usuario.</summary>
    Task<MovimientoCajaTicketDto?> ObtenerAsync(
        int movimientoCajaChicaId,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
