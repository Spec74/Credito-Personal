namespace Credito.Modern.Application.SerieArticulos;

public sealed record SerieArticuloListItemDto(
    int SerieArticuloId,
    string NumeroSerie,
    int AlmacenId,
    int ArticuloId,
    int EstadoId,
    int? MovimientoDetEntId,
    int? MovimientoDetSalId);
