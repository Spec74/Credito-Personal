namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Página de sesiones de caja. El legado paginaba en servidor (jqGrid <c>rowNum</c> + <c>Skip/Take</c>);
/// los totales van del lado del servidor para que no dependan de la página visible.
/// </summary>
public sealed record SaldoCajaSesionPageDto(
    int Page,
    int PageSize,
    int TotalRecords,
    int TotalPages,
    decimal TotalSaldoInicial,
    decimal TotalSaldoFinal,
    IReadOnlyList<SaldoCajaSesionRowDto> Rows);
