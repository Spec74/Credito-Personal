using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF de clientes inactivos con el layout corporativo Credix
/// (<see cref="CredixLegacyPdfDocument"/> / catálogo), mismas columnas que el RDLC.
/// </summary>
public static class RptClientesInactivosFichaPdfDocument
{
    public static byte[] Build(
        IReadOnlyList<RptClientesInactivosRowDto> rows,
        CredixLegacyReportContext? context = null)
    {
        var csv = RptClientesInactivosCsvFormatter.ToUtf8BomCsv(rows);
        return TabularPdfDocument.FromUtf8BomCsv(
            CredixLegacyReportKey.ClientesInactivos,
            csv,
            context ?? new CredixLegacyReportContext
            {
                Titulo = "CLIENTES INACTIVOS",
                Oficina = "TODOS",
            });
    }
}
