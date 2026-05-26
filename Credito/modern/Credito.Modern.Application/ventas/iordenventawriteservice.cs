namespace Credito.Modern.Application.Ventas;

public interface IOrdenVentaWriteService
{
    Task<OrdenVentaOperacionMensajeResponse> AgregarDetalleAsync(
        int ordenVentaId,
        string numeroSerie,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<OrdenVentaOperacionResultResponse> ActualizarDetalleAsync(
        int ordenVentaDetId,
        decimal descuento,
        CancellationToken cancellationToken = default);

    Task<OrdenVentaOperacionResultResponse> EliminarDetalleAsync(
        int ordenVentaDetId,
        CancellationToken cancellationToken = default);

    Task<OrdenVentaOperacionResultResponse> EliminarOrdenAsync(
        int ordenVentaId,
        CancellationToken cancellationToken = default);

    Task<CrearOrdenVentaResponse> CrearAsync(
        int oficinaId,
        int personaId,
        int usuarioId,
        DateTime fechaReg,
        string tipoVenta,
        CancellationToken cancellationToken = default);
}
