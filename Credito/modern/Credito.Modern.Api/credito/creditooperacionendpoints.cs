namespace Credito.Modern.Api.Credito;

/// <summary>Fachada: operación crédito partida por dominio (sin cambiar rutas).</summary>
internal static class CreditoOperacionEndpoints
{
    public static void MapCreditoOperacionEndpoints(this WebApplication app)
    {
        app.MapCreditoSimuladorCarteraEndpoints();
        app.MapCreditoCajaDiarioSesionEndpoints();
        app.MapCreditoFichaGestionEndpoints();
        app.MapCreditoCobroCuotasEndpoints();
        app.MapCreditoCajaDiarioOperacionEndpoints();
        app.MapCreditoBovedaOperacionEndpoints();
        app.MapCreditoCajaChicaOperacionEndpoints();
        app.MapCreditoCicloEndpoints();
    }
}
