namespace Credito.Modern.Application.Almacenes;

/// <summary>Paridad <c>MovimientoBL.ObtenerEntradaSalida</c> para constancia.</summary>
public sealed record RptConstanciaAlmacenCabeceraDto(
    int MovimientoId,
    string Oficina,
    string Almacen,
    string Tipo,
    string TipoMovimiento,
    string TipoMovimientoDesc,
    DateTime Fecha,
    string? Documento,
    string Estado,
    string? Observacion,
    decimal Importe);

/// <summary>Paridad líneas <c>MovimientoDet</c> en vista constancia.</summary>
public sealed record RptConstanciaAlmacenDetLineaDto(
    int Cantidad,
    string Descripcion,
    decimal PrecioUnitario,
    decimal Descuento,
    decimal Importe);

public sealed record RptConstanciaAlmacenDto(
    RptConstanciaAlmacenCabeceraDto Cabecera,
    IReadOnlyList<RptConstanciaAlmacenDetLineaDto> Detalle);
