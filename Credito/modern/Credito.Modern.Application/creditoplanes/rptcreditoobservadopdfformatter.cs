using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>PDF de créditos observados vía ficha profesional Credix.</summary>
public static class RptCreditoObservadoPdfFormatter
{
    public static byte[] ToPdf(IReadOnlyList<RptCreditoObservadoRowDto> rows) =>
        RptCreditoObservadoFichaPdfDocument.Build(
            rows,
            new CredixLegacyReportContext
            {
                Titulo = "CRÉDITOS OBSERVADOS",
                Oficina = "TODOS",
            });
}
