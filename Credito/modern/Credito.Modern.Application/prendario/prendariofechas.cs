namespace Credito.Modern.Application.Prendario;

/// <summary>
/// Fechas del plan prendario ancladas al desembolso (paridad Anexo A/B oficial).
/// </summary>
public static class PrendarioFechas
{
    /// <summary>
    /// Cuota <paramref name="periodos"/> desde el ancla: M=meses, Q=15d, S=7d, D=días.
    /// </summary>
    public static DateTime AvanzarPeriodo(DateTime ancla, string formaPago, int periodos)
    {
        if (periodos < 1)
        {
            return ancla.Date;
        }

        var forma = string.IsNullOrWhiteSpace(formaPago)
            ? 'M'
            : char.ToUpperInvariant(formaPago.Trim()[0]);

        return forma switch
        {
            'D' => ancla.Date.AddDays(periodos),
            'S' => ancla.Date.AddDays(7 * periodos),
            'Q' => ancla.Date.AddDays(15 * periodos),
            _ => ancla.Date.AddMonths(periodos),
        };
    }

    public static DateTime RemateDesdeVencimiento(DateTime fechaVencimiento) =>
        fechaVencimiento.Date.AddDays(30);
}
