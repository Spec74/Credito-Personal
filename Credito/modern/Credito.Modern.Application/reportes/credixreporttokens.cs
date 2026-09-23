using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Reportes;

/// <summary>
/// Estándar visual único para reportes PDF empresariales CrediConfiable.
/// Todos los layout helpers deben consumir estos tokens (no hardcodear colores/textos).
/// </summary>
public static class CredixReportTokens
{
    public const string BrandHex = "#114885";
    public const string BrandDarkHex = "#0A3A66";
    public const string TableHeaderHex = "#B0C4DE";
    public const string TableHeaderStrongHex = "#0F5F8F";
    public const string MetaBgHex = "#F4F7FA";
    public const string SoftBgHex = "#EAF4FB";
    public const string SuccessSoftHex = "#E8F5E9";
    public const string MutedTextHex = "#4B5563";
    public const string BorderHex = "#808080";
    public const string BorderSoftHex = "#B7C7D6";
    public const string ZebraHex = "#F8FBFD";
    public const string TotalsBgHex = "#E8EEF5";

    public const string CompanyLegalName = "Inversiones CrediConfiable";
    public const string CompanyConfidential = "Documento confidencial";
    public const string DateTimeFormat = "dd/MM/yyyy HH:mm";
    public const string DateFormat = "dd/MM/yyyy";

    public const float FontSizeBody = 7.5f;
    public const float FontSizeTitle = 11f;
    public const float FontSizeMeta = 7.5f;
    public const float FontSizeFooter = 7.5f;

    public static readonly Color Brand = Color.FromHex(BrandHex);
    public static readonly Color BrandDark = Color.FromHex(BrandDarkHex);
    public static readonly Color TableHeader = Color.FromHex(TableHeaderHex);
    public static readonly Color TableHeaderStrong = Color.FromHex(TableHeaderStrongHex);
    public static readonly Color MetaBg = Color.FromHex(MetaBgHex);
    public static readonly Color SoftBg = Color.FromHex(SoftBgHex);
    public static readonly Color SuccessSoft = Color.FromHex(SuccessSoftHex);
    public static readonly Color MutedText = Color.FromHex(MutedTextHex);
    public static readonly Color Border = Color.FromHex(BorderHex);
    public static readonly Color BorderSoft = Color.FromHex(BorderSoftHex);
    public static readonly Color Zebra = Color.FromHex(ZebraHex);
    public static readonly Color TotalsBg = Color.FromHex(TotalsBgHex);

    public static readonly CultureInfo Pe = CultureInfo.GetCultureInfo("es-PE");
    public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string NowPrinted() => DateTime.Now.ToString(DateTimeFormat, Pe);

    public static string FormatDate(DateTime value) => value.ToString(DateFormat, Pe);

    public static string FormatDate(DateTime? value) =>
        value is null ? "—" : value.Value.ToString(DateFormat, Pe);

    public static string FormatMoney(decimal value) => value.ToString("N2", Pe);

    public static string FormatMoney(decimal? value) => (value ?? 0m).ToString("N2", Pe);

    /// <summary>Pie estándar: empresa | Página X de Y | confidencial (o filas).</summary>
    public static void ComposeStandardFooter(IContainer container, int? filas = null)
    {
        container
            .DefaultTextStyle(x => x.FontSize(FontSizeFooter).FontColor(Colors.Grey.Darken2))
            .PaddingTop(3)
            .Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    if (filas is int n)
                    {
                        t.Span("Filas: ").FontColor(Colors.Grey.Darken2);
                        t.Span(n.ToString(Inv)).Bold();
                        t.Span("  ·  ");
                    }

                    t.Span(CompanyLegalName);
                });
                row.RelativeItem().AlignCenter().Text(t =>
                {
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
                row.RelativeItem().AlignRight().Text(CompanyConfidential);
            });
    }
}
