using System.Globalization;

namespace Credito.Modern.Application.Dashboard;

/// <summary>
/// Variaciones e insights del tablero. Misma regla que <c>Gestor.cshtml</c>
/// (<c>calcularVariacion</c> / <c>generarAlertas</c>), evaluada en servidor.
/// </summary>
public static class DashboardAnalistaInsights
{
    public const decimal UmbralVariacionPct = 10m;
    public const decimal UmbralMoraCriticaPct = 30m;

    public static decimal? VariacionPorcentaje(decimal actual, decimal anterior)
    {
        if (anterior == 0m)
        {
            return actual == 0m ? 0m : null;
        }

        return ((actual - anterior) / Math.Abs(anterior)) * 100m;
    }

    public static DashboardAnalistaKpisDto ConVariaciones(
        int totalClientes,
        int clientesNuevosActual,
        int clientesNuevosAnterior,
        int creditosActual,
        int creditosAnterior,
        decimal cobradoActual,
        decimal cobradoAnterior,
        decimal saldoActual,
        int clientesMora,
        int porVencerSemana)
    {
        var porcentajeMora = totalClientes > 0
            ? (clientesMora / (decimal)totalClientes) * 100m
            : 0m;

        return new DashboardAnalistaKpisDto(
            TotalClientes: totalClientes,
            ClientesNuevosActual: clientesNuevosActual,
            ClientesNuevosAnterior: clientesNuevosAnterior,
            VariacionClientesNuevosPct: VariacionPorcentaje(clientesNuevosActual, clientesNuevosAnterior),
            CreditosActual: creditosActual,
            CreditosAnterior: creditosAnterior,
            VariacionCreditosPct: VariacionPorcentaje(creditosActual, creditosAnterior),
            CobradoActual: cobradoActual,
            CobradoAnterior: cobradoAnterior,
            VariacionCobradoPct: VariacionPorcentaje(cobradoActual, cobradoAnterior),
            SaldoActual: saldoActual,
            ClientesMora: clientesMora,
            PorcentajeMora: Math.Round(porcentajeMora, 1, MidpointRounding.AwayFromZero),
            PorVencerSemana: porVencerSemana);
    }

    public static IReadOnlyList<DashboardInsightDto> Build(DashboardAnalistaKpisDto kpis)
    {
        var insights = new List<DashboardInsightDto>(4);

        if (kpis.ClientesMora > 0)
        {
            var tipo = kpis.PorcentajeMora >= UmbralMoraCriticaPct ? "danger" : "warning";
            insights.Add(new DashboardInsightDto(
                Tipo: tipo,
                Titulo: "Cartera en mora",
                Mensaje:
                $"Tienes {kpis.ClientesMora} clientes en mora, equivalentes al {FormatoPct(kpis.PorcentajeMora)}% de tu cartera.",
                Accion: "/informes/morosidad-gestor"));
        }
        else
        {
            insights.Add(new DashboardInsightDto(
                Tipo: "success",
                Titulo: "Cartera al día",
                Mensaje: "No tienes clientes registrados en mora.",
                Accion: null));
        }

        if (kpis.PorVencerSemana > 0)
        {
            insights.Add(new DashboardInsightDto(
                Tipo: "warning",
                Titulo: "Próximos vencimientos",
                Mensaje:
                $"{kpis.PorVencerSemana} créditos vencen durante los próximos 7 días. Programa el seguimiento preventivo.",
                Accion: "/informes/morosidad-gestor"));
        }
        else
        {
            insights.Add(new DashboardInsightDto(
                Tipo: "info",
                Titulo: "Sin vencimientos inmediatos",
                Mensaje: "No existen créditos próximos a vencer durante esta semana.",
                Accion: null));
        }

        AgregarVariacion(
            insights,
            kpis.CobradoActual,
            kpis.CobradoAnterior,
            kpis.VariacionCobradoPct,
            tituloSinDatos: "Cobranza del mes",
            mensajeSinDatos: "Todavía no existen cobranzas registradas durante el mes.",
            tituloNuevo: "Nueva cobranza registrada",
            mensajeNuevo: "Ya existen cobranzas este mes, pero el mes anterior no ofrece una base comparable.",
            tituloSube: "Cobranza en crecimiento",
            mensajeSube: "Tu cobranza aumentó {p}% frente al mes anterior.",
            tituloBaja: "Disminución de cobranza",
            mensajeBaja: "Tu cobranza disminuyó {p}% frente al mes anterior. Prioriza la recuperación de cartera.",
            tituloEstable: "Cobranza estable",
            mensajeEstable: "Tu cobranza se mantiene cercana al resultado del mes anterior.",
            tipoBaja: "danger",
            accionBaja: "/informes/cobro-diario");

        AgregarVariacion(
            insights,
            kpis.CreditosActual,
            kpis.CreditosAnterior,
            kpis.VariacionCreditosPct,
            tituloSinDatos: "Créditos colocados",
            mensajeSinDatos: "Todavía no existen créditos colocados durante el mes.",
            tituloNuevo: "Nueva colocación registrada",
            mensajeNuevo: "Ya existen créditos colocados este mes, pero el mes anterior no ofrece una base comparable.",
            tituloSube: "Mayor colocación",
            mensajeSube: "Incrementaste {p}% la cantidad de créditos colocados.",
            tituloBaja: "Menor colocación",
            mensajeBaja: "La cantidad de créditos colocados disminuyó {p}% frente al mes anterior.",
            tituloEstable: "Colocación estable",
            mensajeEstable: "La colocación de créditos se mantiene estable frente al mes anterior.",
            tipoBaja: "warning",
            accionBaja: "/credito/simulador");

        return insights;
    }

    private static void AgregarVariacion(
        List<DashboardInsightDto> insights,
        decimal actual,
        decimal anterior,
        decimal? variacion,
        string tituloSinDatos,
        string mensajeSinDatos,
        string tituloNuevo,
        string mensajeNuevo,
        string tituloSube,
        string mensajeSube,
        string tituloBaja,
        string mensajeBaja,
        string tituloEstable,
        string mensajeEstable,
        string tipoBaja,
        string? accionBaja)
    {
        if (actual <= 0m && anterior <= 0m)
        {
            insights.Add(new DashboardInsightDto("info", tituloSinDatos, mensajeSinDatos, null));
            return;
        }

        if (variacion is null)
        {
            insights.Add(new DashboardInsightDto("success", tituloNuevo, mensajeNuevo, null));
            return;
        }

        var porcentaje = FormatoPct(Math.Abs(variacion.Value));
        if (variacion.Value >= UmbralVariacionPct)
        {
            insights.Add(new DashboardInsightDto(
                "success",
                tituloSube,
                mensajeSube.Replace("{p}", porcentaje, StringComparison.Ordinal),
                null));
            return;
        }

        if (variacion.Value <= -UmbralVariacionPct)
        {
            insights.Add(new DashboardInsightDto(
                tipoBaja,
                tituloBaja,
                mensajeBaja.Replace("{p}", porcentaje, StringComparison.Ordinal),
                accionBaja));
            return;
        }

        insights.Add(new DashboardInsightDto("info", tituloEstable, mensajeEstable, null));
    }

    private static string FormatoPct(decimal valor) =>
        Math.Round(valor, 1, MidpointRounding.AwayFromZero)
            .ToString("0.0", CultureInfo.InvariantCulture);
}
