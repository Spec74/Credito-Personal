using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

public sealed class TipoPagoRulesTests
{
    private const int Bcp = 4;

    [Theory]
    [InlineData(TipoPagoRules.YapeGerente, true, true)]
    [InlineData(TipoPagoRules.YapeRene, true, true)]
    [InlineData(TipoPagoRules.InterbankGerente, true, true)]
    [InlineData(TipoPagoRules.InterbankRene, true, true)]
    [InlineData(TipoPagoRules.Yape, true, false)]
    [InlineData(TipoPagoRules.Interbank, true, false)]
    [InlineData(TipoPagoRules.Plin, true, true)]
    [InlineData(TipoPagoRules.Yape, false, true)]
    [InlineData(TipoPagoRules.Interbank, false, true)]
    [InlineData(TipoPagoRules.YapeGerente, false, false)]
    [InlineData(TipoPagoRules.InterbankRene, false, false)]
    [InlineData(6, true, false)]
    [InlineData(7, false, false)]
    public void RequireCobroPermitido_MatrizSede(int tipoPagoId, bool esCentral, bool permitido)
    {
        var err = TipoPagoRules.RequireCobroPermitido(tipoPagoId, esCentral);
        if (permitido)
            Assert.Null(err);
        else
            Assert.NotNull(err);
    }

    [Fact]
    public void RequireParCobroPermitido_FallaEnPrimerInvalido()
    {
        var err = TipoPagoRules.RequireParCobroPermitido(
            TipoPagoRules.Yape,
            Bcp,
            esCentral: true);
        Assert.NotNull(err);
        Assert.Contains("tipoPagoOrigenId", err, StringComparison.Ordinal);
    }

    [Fact]
    public void RequireParCobroPermitido_OkCuandoAmbosValidos()
    {
        Assert.Null(
            TipoPagoRules.RequireParCobroPermitido(
                TipoPagoRules.YapeGerente,
                Bcp,
                esCentral: true));
    }

    [Fact]
    public void MapFondeoDestino_Familias()
    {
        Assert.Equal(TipoPagoRules.Yape, TipoPagoRules.MapFondeoDestino((short)TipoPagoRules.YapeGerente));
        Assert.Equal(TipoPagoRules.Yape, TipoPagoRules.MapFondeoDestino((short)TipoPagoRules.YapeRene));
        Assert.Equal(TipoPagoRules.Interbank, TipoPagoRules.MapFondeoDestino((short)TipoPagoRules.InterbankGerente));
        Assert.Equal(TipoPagoRules.Interbank, TipoPagoRules.MapFondeoDestino((short)TipoPagoRules.InterbankRene));
        Assert.Equal((short)Bcp, TipoPagoRules.MapFondeoDestino((short)Bcp));
    }

    [Fact]
    public void FamiliasYFlags()
    {
        Assert.True(TipoPagoRules.IsYapeFamily(TipoPagoRules.YapeRene));
        Assert.True(TipoPagoRules.IsInterbankFamily(TipoPagoRules.InterbankGerente));
        Assert.True(TipoPagoRules.IsCentralWallet(TipoPagoRules.YapeGerente));
        Assert.True(TipoPagoRules.IsCentralReplacedGeneric(TipoPagoRules.Yape));
        Assert.False(TipoPagoRules.IsDigital(TipoPagoRules.Efectivo));
        Assert.True(TipoPagoRules.IsDigital(TipoPagoRules.Plin));
    }
}
