using Credito.Modern.Api.Auth;

namespace Credito.Modern.Tests;

public class CreditoAuthorizationPoliciesTests
{
    [Theory]
    [InlineData("CAJA")]
    [InlineData("caja")]
    [InlineData("CAJERO")]
    [InlineData("CAJA CENTRAL")]
    [InlineData("CAJA CENTRAL 1")]
    public void HasRolOperador_acepta_familia_caja(string rol)
    {
        Assert.True(CreditoAuthorizationPolicies.HasRolOperador([rol]));
        Assert.True(CreditoAuthorizationPolicies.HasCajaOperativa([rol]));
    }

    [Theory]
    [InlineData("ANALISTA")]
    [InlineData("GESTOR")]
    [InlineData("ENCARGADO")]
    [InlineData("ADMINISTRADOR")]
    [InlineData("ADMIN")]
    public void HasRolOperador_acepta_roles_operativos(string rol)
    {
        Assert.True(CreditoAuthorizationPolicies.HasRolOperador([rol]));
    }

    [Theory]
    [InlineData("LECTURA")]
    [InlineData("LECTURA_SALDO")]
    [InlineData("REPORTE")]
    [InlineData("REPORTEPARCIAL")]
    [InlineData("ANULACION_MOV")]
    [InlineData("APROBADOR 1")]
    [InlineData("APROBADOR 2")]
    [InlineData("SALDOS CAJA")]
    public void HasRolOperador_rechaza_roles_no_operativos(string rol)
    {
        Assert.False(CreditoAuthorizationPolicies.HasRolOperador([rol]));
    }

    [Fact]
    public void HasSoloLectura_no_incluye_lectura_saldo()
    {
        Assert.True(CreditoAuthorizationPolicies.HasSoloLectura(["LECTURA"]));
        Assert.False(CreditoAuthorizationPolicies.HasSoloLectura(["LECTURA_SALDO"]));
        Assert.True(CreditoAuthorizationPolicies.HasLecturaSaldo(["LECTURA_SALDO"]));
        Assert.False(CreditoAuthorizationPolicies.HasLecturaSaldo(["LECTURA"]));
    }

    [Fact]
    public void HasAprobador_distingue_nivel_1_y_2()
    {
        Assert.True(CreditoAuthorizationPolicies.HasAprobador1(["APROBADOR 1"]));
        Assert.False(CreditoAuthorizationPolicies.HasAprobador1(["APROBADOR 2"]));
        Assert.True(CreditoAuthorizationPolicies.HasAprobador2(["APROBADOR 2"]));
        Assert.False(CreditoAuthorizationPolicies.HasAprobador2(["APROBADOR 1"]));
        Assert.True(CreditoAuthorizationPolicies.HasAprobador(["APROBADOR 2"]));
        Assert.False(CreditoAuthorizationPolicies.HasAprobador1OAdministrador(["APROBADOR 2"]));
        Assert.True(CreditoAuthorizationPolicies.HasAprobador1OAdministrador(["APROBADOR 1"]));
    }

    [Fact]
    public void HasEncargado_no_confunde_con_cajero_encargado_compuesto()
    {
        // Claim compuesto histórico: no es el rol ENCARGADO.
        Assert.False(CreditoAuthorizationPolicies.HasEncargado(["CAJERO ENCARGADO"]));
        Assert.True(CreditoAuthorizationPolicies.HasCajaOperativa(["CAJERO ENCARGADO"]));
    }

    [Fact]
    public void HasAnulacionMovimientoCaja_admin_o_flag()
    {
        Assert.True(CreditoAuthorizationPolicies.HasAnulacionMovimientoCaja(["ANULACION_MOV"]));
        Assert.True(CreditoAuthorizationPolicies.HasAnulacionMovimientoCaja(["ADMINISTRADOR"]));
        Assert.False(CreditoAuthorizationPolicies.HasAnulacionMovimientoCaja(["CAJA"]));
    }
}
