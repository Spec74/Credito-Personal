using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

public class CreditoRechazoReglasTests
{
    [Theory]
    [InlineData("PEN", false, null)]
    [InlineData("pen", false, null)]
    [InlineData(" PEN ", false, null)]
    public void Pendiente_sin_movimientos_se_puede_rechazar(string estado, bool financieros, string? esperado)
    {
        Assert.Equal(esperado, CreditoRechazoReglas.MensajeSiNoRechazable(estado, financieros));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("APR")]
    [InlineData("DES")]
    [InlineData("ANU")]
    public void No_pendiente_pide_anular(string? estado)
    {
        var msg = CreditoRechazoReglas.MensajeSiNoRechazable(estado, false);
        Assert.Contains("anular", msg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PEN", msg, StringComparison.Ordinal);
    }

    [Fact]
    public void Pendiente_con_caja_o_cxc_pide_anular()
    {
        var msg = CreditoRechazoReglas.MensajeSiNoRechazable("PEN", true);
        Assert.Contains("cuentas por cobrar", msg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("anular", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fk_sql_se_traduce()
    {
        var ex = new InvalidOperationException(
            "The DELETE statement conflicted with the REFERENCE constraint \"FK__CreditoEx__Credi__5AFA3B08\".");
        Assert.Contains("registros relacionados", CreditoRechazoReglas.MensajeDesdeDb(ex), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Error_generico_no_filtra_sql()
    {
        Assert.Equal(
            "No se pudo rechazar el crédito.",
            CreditoRechazoReglas.MensajeDesdeDb(new InvalidOperationException("timeout")));
    }
}
