namespace Credito.Modern.Api.Credito;

/// <summary>Fachada: reportes crédito restantes partidos por dominio (sin cambiar rutas).</summary>
internal static class CreditoReportesRestantesEndpoints
{
    public static void MapCreditoReportesRestantesEndpoints(this WebApplication app)
    {
        app.MapCreditoReporteCentralRiesgoSaldoCarteraEndpoints();
        app.MapCreditoReporteReajustesEndpoints();
        app.MapCreditoReporteCobroDiarioEndpoints();
        app.MapCreditoReporteClientesEndpoints();
        app.MapCreditoReporteCajaBovedaEndpoints();
        app.MapCreditoReporteCreditoCarteraEndpoints();
        app.MapCreditoReporteCajaOperacionEndpoints();
        app.MapCreditoReporteCondonacionPagosEndpoints();
    }
}
