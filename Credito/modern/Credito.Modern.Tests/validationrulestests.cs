using Credito.Modern.Application.Clientes;
using Credito.Modern.Application.Validation;

namespace Credito.Modern.Tests;

public sealed class ValidationRulesTests
{
    [Fact]
    public void IdRules_RejectsNonPositive()
    {
        Assert.NotNull(IdRules.RequirePositive(0, "oficinaId"));
        Assert.Null(IdRules.RequirePositive(1, "oficinaId"));
        Assert.NotNull(IdRules.RequireAllPositive([], "ids"));
        Assert.NotNull(IdRules.RequireAllPositive([1, 0], "ids"));
        Assert.Null(IdRules.RequireAllPositive([1, 2], "ids"));
    }

    [Fact]
    public void MoneyRules_EnforcesTwoDecimalsAndPositive()
    {
        Assert.NotNull(MoneyRules.RequirePositive(0, "importe"));
        Assert.NotNull(MoneyRules.RequirePositive(1.001m, "importe"));
        Assert.Null(MoneyRules.RequirePositive(10.50m, "importe"));
        Assert.Null(MoneyRules.RequireNonNegative(0, "descuento"));
    }

    [Fact]
    public void StringRules_DocumentsAndCelular()
    {
        Assert.Null(StringRules.RequireDni("12345678"));
        Assert.NotNull(StringRules.RequireDni("123"));
        Assert.Null(StringRules.RequireRuc("20123456789"));
        Assert.True(StringRules.EsCelularPeruano("987654321"));
        Assert.NotNull(StringRules.OptionalCelularPeruano("187654321"));
        Assert.Null(StringRules.OptionalCelularPeruano(null));
    }

    [Fact]
    public void StringRules_DireccionRealista_RechazaBasura()
    {
        Assert.Null(StringRules.OptionalDireccionRealista(null));
        Assert.Null(StringRules.OptionalDireccionRealista(""));
        Assert.NotNull(StringRules.OptionalDireccionRealista("123"));
        Assert.NotNull(StringRules.OptionalDireccionRealista("99999999"));
        Assert.NotNull(StringRules.OptionalDireccionRealista("abc"));
        Assert.Null(StringRules.OptionalDireccionRealista("Jr. Lima 245"));
        Assert.Null(StringRules.OptionalDireccionRealista("Av. Centenario mz A lt 12"));
    }

    [Fact]
    public void DateRules_FechaNacimiento_NoFuturaNiMenorDeEdad()
    {
        var hoy = new DateTime(2026, 10, 8);
        Assert.Null(DateRules.OptionalFechaNacimiento(null, hoy));
        Assert.NotNull(DateRules.OptionalFechaNacimiento(hoy.AddDays(1), hoy));
        Assert.NotNull(DateRules.OptionalFechaNacimiento(hoy.AddYears(-17), hoy));
        Assert.Null(DateRules.OptionalFechaNacimiento(hoy.AddYears(-25), hoy));
        Assert.NotNull(DateRules.OptionalFechaNacimiento(hoy.AddYears(-130), hoy));
        Assert.NotNull(DateRules.RequireFechaNacimiento(null, hoy));
        Assert.Null(DateRules.RequireFechaNacimiento(hoy.AddYears(-25), hoy));
    }

    [Fact]
    public void MaestroValidacion_DenominacionMaxLength()
    {
        Assert.NotNull(MaestroValidacion.ValidarDenominacion("  "));
        Assert.Null(MaestroValidacion.ValidarDenominacion("Marca X"));
        Assert.NotNull(MaestroValidacion.ValidarDenominacion(new string('A', 101)));
    }

    [Fact]
    public void ClienteValidacion_RequiresNaturalPersonFields()
    {
        var req = new GuardarClienteRequest(
            ClienteId: 0,
            TipoPersona: "N",
            Nombre: "Juan",
            ApePaterno: null,
            ApeMaterno: null,
            NumeroDocumento: "12345678",
            SexoMasculino: true,
            Email: null,
            Celular1: "987654321",
            Nota: null,
            FechaNacimiento: null,
            Direccion: null,
            DireccionRef: null,
            DistritoId: null,
            DireccionNegocio: null,
            DireccionNegocioRef: null,
            Latitud: null,
            Longitud: null,
            OcupacionId: null,
            OcupacionOtros: null,
            Calificacion: "A",
            Activo: true,
            TopeCredito: null,
            EstadoCivilId: null,
            TipoViviendaId: null,
            ConyuguePersonaId: null,
            ClasificacionRiesgoSbsId: null,
            ClasificacionRiesgoSbsObs: null);

        var error = ClienteValidacion.ValidarGuardar(req);
        Assert.NotNull(error);
        Assert.Contains("apellido", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TareaValidacion_RequiresSubtarea()
    {
        Assert.NotNull(TareaValidacion.ValidarGuardar(1, []));
        Assert.NotNull(TareaValidacion.ValidarGuardar(0, ["Seguimiento"]));
        Assert.Null(TareaValidacion.ValidarGuardar(10, ["Seguimiento"]));
    }

    [Fact]
    public void ValidationGate_ReturnsFirstError()
    {
        Assert.Equal("a", ValidationGate.First(null, "a", "b"));
        Assert.Null(ValidationGate.First(null, null));
    }
}
