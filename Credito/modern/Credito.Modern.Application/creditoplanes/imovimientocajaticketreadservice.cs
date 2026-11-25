namespace Credito.Modern.Application.CreditoPlanes;

public interface IMovimientoCajaTicketReadService
{
    Task<MovimientoCajaTicketDto?> ObtenerAsync(int movimientoCajaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ticket de cobro mixto: usa el último movimiento para saldos/estado y suma todos los medios.
    /// Si la lista tiene un solo id, equivale a <see cref="ObtenerAsync"/>.
    /// </summary>
    Task<MovimientoCajaTicketDto?> ObtenerGrupoAsync(
        IReadOnlyList<int> movimientoCajaIds,
        CancellationToken cancellationToken = default);
}
