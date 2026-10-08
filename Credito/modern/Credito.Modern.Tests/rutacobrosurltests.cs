using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Infrastructure.CreditoPlanes;

namespace Credito.Modern.Tests;

public class RutaCobrosUrlTests
{
    [Fact]
    public void ConstruirUrlNavegacionGoogle_con_origen_y_dos_paradas()
    {
        var paradas = new List<RutaCobroParadaDto>
        {
            new(1, 10, "Ana", 100m, null, -13.16m, -74.22m, true),
            new(2, 11, "Luis", 200m, null, -13.17m, -74.23m, true),
        };

        var url = RutaCobrosService.ConstruirUrlNavegacionGoogle(-13.15m, -74.21m, paradas);

        Assert.NotNull(url);
        Assert.StartsWith("https://www.google.com/maps/dir/?api=1", url);
        Assert.Contains("origin=", url);
        Assert.Contains("destination=", url);
        Assert.Contains("waypoints=", url);
        Assert.Contains("travelmode=driving", url);
    }

    [Fact]
    public void ConstruirUrlNavegacionGoogle_sin_gps_devuelve_null()
    {
        var paradas = new List<RutaCobroParadaDto>
        {
            new(1, 10, "Ana", 100m, "Jr. Lima", null, null, false),
        };

        Assert.Null(RutaCobrosService.ConstruirUrlNavegacionGoogle(null, null, paradas));
    }
}
