using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Prendario;

namespace Credito.Modern.Tests;

public sealed class PrendarioValidacionTests
{
    [Fact]
    public void ValidarPrendas_RechazaListaVacia()
    {
        var error = PrendarioValidacion.ValidarPrendas([]);
        Assert.NotNull(error);
        Assert.Contains("al menos un bien", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidarPrendas_RechazaTasacionCero()
    {
        var error = PrendarioValidacion.ValidarPrendas(
        [
            new PrendaItemRequest("Moto", null, null, null, null, 0, null, null),
        ]);
        Assert.NotNull(error);
        Assert.Contains("tasación", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidarPrendas_AceptaBienValido()
    {
        var error = PrendarioValidacion.ValidarPrendas(
        [
            new PrendaItemRequest("Moto KTM", "KTM", "Duke", "N/T", "Naranja", 4500.50m, null, null),
        ]);
        Assert.Null(error);
    }

    [Fact]
    public void ValidarGeneracionPrendaria_ExigeMensualYUnaCuota()
    {
        var error = PrendarioValidacion.ValidarGeneracionPrendaria(
            "Q",
            1,
            1000m,
            2000m,
            DateTime.Today.AddDays(30));
        Assert.NotNull(error);
        Assert.Contains("mensual", error, StringComparison.OrdinalIgnoreCase);

        error = PrendarioValidacion.ValidarGeneracionPrendaria(
            "M",
            3,
            1000m,
            2000m,
            DateTime.Today.AddDays(30));
        Assert.NotNull(error);
        Assert.Contains("1 cuota", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidarGeneracionPrendaria_MontoNoSuperaTasacion()
    {
        var error = PrendarioValidacion.ValidarGeneracionPrendaria(
            "M",
            1,
            5000m,
            3000m,
            DateTime.Today.AddDays(30));
        Assert.NotNull(error);
        Assert.Contains("tasación", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EsCelularPeruano_ValidaFormato()
    {
        Assert.True(PrendarioValidacion.EsCelularPeruano("987654321"));
        Assert.True(PrendarioValidacion.EsCelularPeruano("987-654-321"));
        Assert.False(PrendarioValidacion.EsCelularPeruano("187654321"));
        Assert.False(PrendarioValidacion.EsCelularPeruano("98765"));
        Assert.False(PrendarioValidacion.EsCelularPeruano(null));
    }
}
