using Credito.Modern.Application.Prendario;

namespace Credito.Modern.Tests;

public sealed class DesembolsoPrendarioFechasTests
{
    [Theory]
    [InlineData("M", 1, "2025-01-30", "2025-02-28")]
    [InlineData("M", 2, "2025-01-30", "2025-03-30")]
    [InlineData("Q", 1, "2025-01-30", "2025-02-14")]
    [InlineData("S", 1, "2025-01-30", "2025-02-06")]
    [InlineData("D", 1, "2025-01-30", "2025-01-31")]
    public void AvanzarPeriodo_desde_desembolso(string forma, int n, string anclaIso, string esperadoIso)
    {
        var ancla = DateTime.Parse(anclaIso);
        var esperado = DateTime.Parse(esperadoIso);
        Assert.Equal(esperado, PrendarioFechas.AvanzarPeriodo(ancla, forma, n));
    }

    [Fact]
    public void Remate_coincide_con_documento_oficial_enero_2025()
    {
        var desembolso = new DateTime(2025, 1, 30);
        var venc = PrendarioFechas.AvanzarPeriodo(desembolso, "M", 1);
        var remate = PrendarioFechas.RemateDesdeVencimiento(venc);
        Assert.Equal(new DateTime(2025, 2, 28), venc);
        Assert.Equal(new DateTime(2025, 3, 30), remate);
    }
}
