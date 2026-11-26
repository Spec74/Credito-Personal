namespace Credito.Modern.Application.MediosPago;

public sealed record OficinaMedioPagoDto(
    int OficinaMedioPagoId,
    int OficinaId,
    int? ParentOficinaMedioPagoId,
    short TipoPagoId,
    string Nombre,
    string MarcaCodigo,
    int Orden,
    bool Activo,
    bool EsDigital,
    bool UsaCelular,
    string? Telefono,
    bool UsaCuenta,
    string? NumeroCuenta,
    string? Cci,
    bool UsaQr,
    string? QrEmvPayload,
    string? QrNota,
    string? FamiliaCodigo,
    bool SoloCentral,
    IReadOnlyList<OficinaMedioPagoDto> Variantes);

public sealed record GuardarOficinaMedioPagoRequest(
    int? OficinaMedioPagoId,
    int OficinaId,
    int? ParentOficinaMedioPagoId,
    short? TipoPagoId,
    string Nombre,
    string MarcaCodigo,
    int Orden,
    bool Activo,
    bool EsDigital,
    bool UsaCelular,
    string? Telefono,
    bool UsaCuenta,
    string? NumeroCuenta,
    string? Cci,
    bool UsaQr,
    string? QrEmvPayload,
    string? QrNota,
    string? FamiliaCodigo,
    bool SoloCentral,
    /// <summary>Si true y no hay TipoPagoId, crea ItemId nuevo en ValorTabla 13.</summary>
    bool CrearTipoPagoCatalogo);

public sealed record OficinaMedioPagoOperacionResponse(int OficinaMedioPagoId, short TipoPagoId);
