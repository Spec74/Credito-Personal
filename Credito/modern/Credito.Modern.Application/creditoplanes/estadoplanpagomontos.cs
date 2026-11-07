namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Montos de visualización del plan. En <c>usp_EstadoPlanPago</c>,
/// <see cref="EstadoPlanPagoCuotaDto.PagoCuota"/> en PEN es saldo a pagar
/// (desde CuotasPendientes), no el importe cobrado; el abono parcial está en PagoLibre.
/// </summary>
public static class EstadoPlanPagoMontos
{
    public static decimal ImportePagado(EstadoPlanPagoCuotaDto row)
    {
        var est = (row.Estado ?? string.Empty).Trim().ToUpperInvariant();
        if (est == "PAG")
        {
            return decimal.Round(row.PagoCuota ?? 0m, 2, MidpointRounding.AwayFromZero);
        }

        return decimal.Round(row.PagoLibre, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal TotalPagado(IEnumerable<EstadoPlanPagoCuotaDto> rows) =>
        rows.Sum(ImportePagado);

    /// <summary>
    /// Saldo pendiente de la cuota (incluye mora/cargos que ya vienen en PagoCuota PEN).
    /// </summary>
    public static decimal SaldoPorPagar(EstadoPlanPagoCuotaDto row)
    {
        var est = (row.Estado ?? string.Empty).Trim().ToUpperInvariant();
        if (est == "PAG")
        {
            return 0m;
        }

        if (row.PagoCuota.HasValue)
        {
            return decimal.Round(Math.Max(0m, row.PagoCuota.Value), 2, MidpointRounding.AwayFromZero);
        }

        var estimado = row.Cuota - row.PagoLibre + (row.ImporteMora ?? 0m) + (row.Cargo ?? 0m);
        return decimal.Round(Math.Max(0m, estimado), 2, MidpointRounding.AwayFromZero);
    }

    public static decimal TotalSaldoPorPagar(IEnumerable<EstadoPlanPagoCuotaDto> rows) =>
        rows.Sum(SaldoPorPagar);
}
