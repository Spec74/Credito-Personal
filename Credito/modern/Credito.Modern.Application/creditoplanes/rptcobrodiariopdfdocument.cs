using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF de cobro diario / morosidad por gestor con layout corporativo Credix
/// (<see cref="CredixLegacyPdfDocument"/> / catálogo).
/// </summary>
public static class RptCobroDiarioPdfDocument
{
    public static byte[] Build(
        IReadOnlyList<RptCobroDiarioRowDto> rows,
        CredixLegacyReportContext context,
        bool soloMora)
    {
        var key = soloMora
            ? CredixLegacyReportKey.MorosidadGestor
            : CredixLegacyReportKey.CobroDiario;
        var def = CredixLegacyReportCatalog.Get(key);
        var ctx = context with
        {
            Titulo = string.IsNullOrWhiteSpace(context.Titulo) ? def.Title : context.Titulo,
        };
        var csv = RptCobroDiarioCsvFormatter.ToUtf8BomCsv(rows);
        return TabularPdfDocument.FromUtf8BomCsv(key, csv, ctx);
    }
}
