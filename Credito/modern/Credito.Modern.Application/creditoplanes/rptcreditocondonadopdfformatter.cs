using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF de créditos condonados vía chrome estándar Credix (paridad datos CSV/JSON).
/// </summary>
public static class RptCreditoCondonadoPdfFormatter
{
    public static byte[] ToPdf(IReadOnlyList<RptCreditoCondonadoRowDto> rows)
    {
        var csv = RptCreditoCondonadoCsvFormatter.ToUtf8BomCsv(rows);
        return TabularPdfDocument.FromUtf8BomCsv(
            "Creditos condonados",
            csv,
            context: new CredixLegacyReportContext
            {
                Titulo = "CRÉDITOS CONDONADOS",
                Referencia = $"Filas: {rows.Count}",
            });
    }
}
