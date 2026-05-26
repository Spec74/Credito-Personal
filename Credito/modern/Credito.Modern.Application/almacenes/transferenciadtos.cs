namespace Credito.Modern.Application.Almacenes;

public sealed record TransferenciaListRowDto(
    int TransferenciaId,
    string AlmacenOrigen,
    string AlmacenDestino,
    DateTime Fecha,
    string Estado);

public sealed record TransferenciaListPageDto(
    IReadOnlyList<TransferenciaListRowDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record TransferenciaCabeceraDto(
    int TransferenciaId,
    int AlmacenOrigenId,
    int AlmacenDestinoId,
    string AlmacenOrigen,
    string AlmacenDestino,
    DateTime Fecha,
    string Estado,
    bool Editable);

public sealed record TransferenciaDetalleLineaDto(
    int TransferenciaId,
    int ArticuloId,
    string Articulo,
    int Cantidad,
    string Series);

public sealed record ValidarSerieTransferenciaResponse(
    bool Error,
    string? Mensaje,
    int? SerieId,
    string? NumeroSerie,
    int? ArticuloId,
    string? Denominacion);

public sealed record CrearTransferenciaResponse(int TransferenciaId);

public sealed record ConfirmarTransferenciaResponse(bool Success, string Mensaje);

public sealed record TransferenciaOperacionRequest(int OficinaId, int TransferenciaId);

public sealed record CrearTransferenciaRequest(int OficinaId, int AlmacenDestinoId);

public sealed record ValidarSerieTransferenciaRequest(
    int OficinaId,
    int TransferenciaId,
    string NumeroSerie);

public sealed record EliminarSerieTransferenciaRequest(
    int OficinaId,
    int TransferenciaId,
    int ArticuloId);
