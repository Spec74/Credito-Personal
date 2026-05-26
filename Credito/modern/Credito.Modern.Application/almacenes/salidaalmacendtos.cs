namespace Credito.Modern.Application.Almacenes;

public sealed record BuscarSerieSalidaResponse(
    bool Error,
    string? Mensaje,
    int? SerieId,
    string? Serie,
    int? ArticuloId,
    string? Denominacion);

public sealed record SerieSalidaLineaRequest(
    int SerieId,
    string Serie,
    int ArticuloId,
    string Denominacion);

public sealed record RealizarSalidaRequest(
    int OficinaId,
    int TipoMovimientoId,
    string Glosa,
    IReadOnlyList<SerieSalidaLineaRequest> Series);

public sealed record RealizarSalidaResponse(int MovimientoId);
