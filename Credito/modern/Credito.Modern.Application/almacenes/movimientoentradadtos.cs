namespace Credito.Modern.Application.Almacenes;

public sealed record MovimientoEntradaListRowDto(
    int MovimientoId,
    string Tipo,
    int TipoMovimientoId,
    string TipoMovimiento,
    DateTime Fecha,
    string? Documento,
    string Estado,
    string? Observacion);

public sealed record MovimientoEntradaListPageDto(
    IReadOnlyList<MovimientoEntradaListRowDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record MovimientoEntradaCabeceraDto(
    int MovimientoId,
    int AlmacenId,
    int OficinaId,
    string Almacen,
    string Tipo,
    int TipoMovimientoId,
    string TipoMovimiento,
    DateTime Fecha,
    string? Documento,
    string Estado,
    int EstadoId,
    string? Observacion,
    decimal SubTotal,
    decimal Igv,
    decimal AjusteRedondeo,
    decimal TotalImporte,
    bool Editable);

public sealed record MovimientoDocRowDto(
    int MovimientoDocId,
    int TipoDocumentoId,
    string TipoDocumento,
    string SerieDocumento,
    string NroDocumento,
    bool PuedeEliminar);

public sealed record MovimientoEntradaDetLineaDto(
    int MovimientoDetId,
    int ArticuloId,
    int Cantidad,
    int UnidadMedidaT10,
    string UnidadMedida,
    string Descripcion,
    decimal PrecioUnitario,
    decimal Descuento,
    decimal Importe,
    bool IndCorrelativo,
    bool PuedeEliminar);

public sealed record MovimientoEntradaDetalleResponse(
    MovimientoEntradaCabeceraDto Cabecera,
    IReadOnlyList<MovimientoDocRowDto> Documentos,
    IReadOnlyList<MovimientoEntradaDetLineaDto> Detalle);

public sealed record CrearMovimientoRequest(
    int OficinaId,
    int AlmacenId,
    int TipoMovimientoId);

public sealed record CrearMovimientoResponse(int MovimientoId);

public sealed record AgregarMovimientoDocumentoRequest(
    int OficinaId,
    int MovimientoId,
    int TipoDocumentoId,
    string SerieDocumento,
    string NroDocumento);

public sealed record EliminarMovimientoDocumentoRequest(int OficinaId, int MovimientoDocId);

public sealed record ActualizarImporteMovimientoRequest(
    int OficinaId,
    int MovimientoId,
    decimal AjusteRedondeo);
