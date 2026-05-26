namespace Credito.Modern.Application.Maestros;

public sealed record MaestroOperacionResponse(bool Success, int? Id, string? Mensaje);

public sealed record GuardarMarcaRequest(int MarcaId, string Denominacion, bool Estado);

public sealed record GuardarModeloRequest(
    int ModeloId,
    int MarcaId,
    string Denominacion,
    bool Estado);

public sealed record GuardarTipoArticuloRequest(
    int TipoArticuloId,
    string Denominacion,
    string? Descripcion,
    bool Estado);

public sealed record GuardarOficinaRequest(
    int OficinaId,
    string Denominacion,
    string? Descripcion,
    string? Telefono,
    int UsuarioAsignadoId,
    bool Estado,
    bool IndPrincipal,
    decimal? Latitud = null,
    decimal? Longitud = null);

public sealed record GuardarAlmacenRequest(
    int AlmacenId,
    int OficinaId,
    string Denominacion,
    string? Descripcion,
    bool Estado);

public sealed record GuardarListaPrecioRequest(
    int ListaPrecioId,
    int ArticuloId,
    decimal Monto,
    decimal Descuento,
    int? Puntos,
    int? PuntosCanje,
    bool Estado);

public sealed record OficinaAdminListItemDto(
    int OficinaId,
    string? Denominacion,
    string? Descripcion,
    string? Telefono,
    int UsuarioAsignadoId,
    bool IndPrincipal,
    bool Estado,
    decimal? Latitud = null,
    decimal? Longitud = null);

public sealed record AlmacenAdminListItemDto(
    int AlmacenId,
    int OficinaId,
    string? OficinaDenominacion,
    string Denominacion,
    string? Descripcion,
    bool Estado);

public sealed record ModeloAdminListItemDto(
    int ModeloId,
    string Denominacion,
    int? MarcaId,
    string? MarcaDenominacion,
    bool Estado);
