namespace Credito.Modern.Application.CreditoPlanes;

public sealed record BovedaListadoResultDto(
    IReadOnlyList<BovedaListadoRowDto> Rows,
    int Total,
    int Page,
    int PageSize);
