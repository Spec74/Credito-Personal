namespace Credito.Modern.Application.Almacenes;

public interface IMovimientoAlmacenWriteService
{
    Task<MovimientoOperacionMensajeResponse> ConfirmarAsync(
        int movimientoId,
        CancellationToken cancellationToken = default);

    Task<MovimientoOperacionMensajeResponse> DesconfirmarAsync(
        int movimientoId,
        CancellationToken cancellationToken = default);

    Task ActualizarAsync(
        int movimientoId,
        int tipoMovimientoId,
        DateTime fecha,
        string observacion,
        CancellationToken cancellationToken = default);

    Task CrearDetalleAsync(
        int movimientoId,
        int movimientoDetId,
        int articuloId,
        bool indAutogenerar,
        string listaSerie,
        int cantidad,
        bool indCorrelativo,
        decimal precioUnitario,
        decimal descuento,
        int medida,
        CancellationToken cancellationToken = default);

    Task EliminarDetalleAsync(int movimientoDetId, CancellationToken cancellationToken = default);

    Task<CrearMovimientoResponse> CrearMovimientoAsync(
        int almacenId,
        int tipoMovimientoId,
        DateTime fecha,
        CancellationToken cancellationToken = default);

    Task AgregarDocumentoAsync(
        int movimientoId,
        int tipoDocumentoId,
        string serieDocumento,
        string nroDocumento,
        CancellationToken cancellationToken = default);

    Task EliminarDocumentoAsync(int movimientoDocId, CancellationToken cancellationToken = default);

    Task ActualizarImporteAsync(
        int movimientoId,
        decimal ajusteRedondeo,
        CancellationToken cancellationToken = default);
}
