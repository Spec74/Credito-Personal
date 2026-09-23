using Credito.Modern.Application.Dashboard;

namespace Credito.Modern.Tests;

public class DashboardAnalistaInsightsTests
{
    [Fact]
    public void Variacion_sin_base_anterior_es_nula_si_hay_actual()
    {
        Assert.Null(DashboardAnalistaInsights.VariacionPorcentaje(10m, 0m));
        Assert.Equal(0m, DashboardAnalistaInsights.VariacionPorcentaje(0m, 0m));
        Assert.Equal(50m, DashboardAnalistaInsights.VariacionPorcentaje(150m, 100m));
        Assert.Equal(-20m, DashboardAnalistaInsights.VariacionPorcentaje(80m, 100m));
    }

    [Fact]
    public void Insights_mora_critica_y_vencimientos()
    {
        var kpis = DashboardAnalistaInsights.ConVariaciones(
            totalClientes: 10,
            clientesNuevosActual: 1,
            clientesNuevosAnterior: 1,
            creditosActual: 2,
            creditosAnterior: 2,
            cobradoActual: 100m,
            cobradoAnterior: 100m,
            cobradoHoy: 10m,
            cobradoAyer: 8m,
            saldoActual: 5000m,
            montoMora: 1200m,
            clientesMora: 4,
            clientesMoraSinPago: 3,
            clientesMoraNuncaPagaron: 2,
            clientesMoraDejaronPagar: 1,
            clientesMoraPagandoConAtraso: 1,
            porVencerSemana: 3);

        Assert.Equal(40.0m, kpis.PorcentajeMora);
        var insights = DashboardAnalistaInsights.Build(kpis);
        Assert.Contains(insights, i => i.Tipo == "danger" && i.Titulo == "Cartera en mora");
        Assert.Contains(insights, i => i.Accion == "/informes/morosidad-gestor" && i.Titulo == "Próximos vencimientos");
    }

    [Fact]
    public void Insights_cartera_al_dia_sin_vencimientos()
    {
        var kpis = DashboardAnalistaInsights.ConVariaciones(
            totalClientes: 8,
            clientesNuevosActual: 0,
            clientesNuevosAnterior: 0,
            creditosActual: 0,
            creditosAnterior: 0,
            cobradoActual: 0m,
            cobradoAnterior: 0m,
            cobradoHoy: 0m,
            cobradoAyer: 0m,
            saldoActual: 1000m,
            montoMora: 0m,
            clientesMora: 0,
            clientesMoraSinPago: 0,
            clientesMoraNuncaPagaron: 0,
            clientesMoraDejaronPagar: 0,
            clientesMoraPagandoConAtraso: 0,
            porVencerSemana: 0);

        var insights = DashboardAnalistaInsights.Build(kpis);
        Assert.Contains(insights, i => i.Tipo == "success" && i.Titulo == "Cartera al día");
        Assert.Contains(insights, i => i.Tipo == "info" && i.Titulo == "Sin vencimientos inmediatos");
        Assert.Contains(insights, i => i.Titulo == "Cobranza del mes");
        Assert.Contains(insights, i => i.Titulo == "Créditos colocados");
    }

    [Fact]
    public void Insights_caida_de_cobranza_es_danger()
    {
        var kpis = DashboardAnalistaInsights.ConVariaciones(
            totalClientes: 5,
            clientesNuevosActual: 0,
            clientesNuevosAnterior: 0,
            creditosActual: 4,
            creditosAnterior: 2,
            cobradoActual: 70m,
            cobradoAnterior: 100m,
            cobradoHoy: 5m,
            cobradoAyer: 20m,
            saldoActual: 1m,
            montoMora: 0m,
            clientesMora: 0,
            clientesMoraSinPago: 0,
            clientesMoraNuncaPagaron: 0,
            clientesMoraDejaronPagar: 0,
            clientesMoraPagandoConAtraso: 0,
            porVencerSemana: 0);

        Assert.Equal(-30m, kpis.VariacionCobradoPct);
        Assert.Equal(100m, kpis.VariacionCreditosPct);
        var insights = DashboardAnalistaInsights.Build(kpis);
        Assert.Contains(
            insights,
            i => i.Tipo == "danger" && i.Titulo == "Disminución de cobranza" && i.Accion == "/informes/cobro-diario");
        Assert.Contains(insights, i => i.Tipo == "success" && i.Titulo == "Mayor colocación");
    }
}
