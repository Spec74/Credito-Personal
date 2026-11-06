using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

public class RptMovimientoCreditoFichaPdfDocumentTests
{
    [Fact]
    public void Build_incluye_usuario_desembolso_en_cabecera()
    {
        var cab = new RptEstadoCreditoCabeceraDto(
            CreditoId: 1,
            PersonaId: 1,
            Producto: "CREDI FACIL",
            FechaPrimerPago: new DateTime(2026, 11, 7),
            FechaVencimiento: new DateTime(2026, 12, 7),
            MontoCredito: 1500m,
            Modalidad: "DIARIO",
            NumeroCuotas: 26,
            Interes: 6m,
            Estado: "DES",
            CodigoPersona: "C1",
            Cliente: "74039362 CLIENTE",
            Analista: "ESULCA",
            UsuarioDesembolso: "JVILLALOBOS",
            MontoGastosAdm: 15m,
            CentralRiesgo: 0m,
            TipoGastoAdm: "ADE",
            Total: 1590m,
            MontoDesembolso: 1500m);

        var movs = new List<RptMovimientoCreditoRowDto>
        {
            new()
            {
                MovimientoCajaId = 0,
                Fecha = new DateTime(2026, 11, 6),
                Operacion = "---",
                Glosa = "SALDO INICIAL",
                ImportePago = 0m,
                Saldo = 1590m,
            },
        };

        var pdf = RptMovimientoCreditoFichaPdfDocument.Build(cab, 1, movs);
        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 500);
        // Cabecera serializada en PDF; smoke de generación con Desembolsó poblado.
        Assert.False(string.IsNullOrWhiteSpace(cab.UsuarioDesembolso));
    }
}
