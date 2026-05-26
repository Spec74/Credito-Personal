namespace Credito.Modern.Application.TipoOperaciones;

public sealed record TipoOperacionListItemDto(
    int TipoOperacionId,
    string Codigo,
    string Denominacion,
    bool IndEntrada,
    bool IndCajaDiario,
    bool IndBoveda,
    bool IndCajaChica);
