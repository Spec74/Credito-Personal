using System.Globalization;
using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Plan de pagos del simulador. Plantilla profesional QuestPDF inspirada en el
/// RDLC solicitado por gerencia, pero mantenible para la plataforma moderna.
/// </summary>
public static class RptSimuladorPlanPagosPdfDocument
{
    private enum IconKind
    {
        Client,
        Money,
        Conditions,
        Calendar,
        Briefcase,
        Card,
        Percent,
        Alert,
        SignatureClient,
        SignatureAdvisor,
    }

    private static readonly Color Brand = Color.FromHex("#004784");
    private static readonly Color BrandDark = Color.FromHex("#003A6C");
    private static readonly Color BrandSoft = Color.FromHex("#EAF4FB");
    private static readonly Color Border = Color.FromHex("#AFC3D8");
    private static readonly Color TableHeader = Color.FromHex("#0B4F8A");
    private static readonly Color Muted = Color.FromHex("#4B5563");
    private static readonly Color Zebra = Color.FromHex("#F8FBFD");

    static RptSimuladorPlanPagosPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(RptSimuladorPlanPagosInformeDto informe)
    {
        var cab = informe.Cabecera;
        var culture = CultureInfo.CurrentCulture;
        var inv = CultureInfo.InvariantCulture;
        var logo = CredixReportAssets.LoadLogo();
        var fechaEmision = DateTime.Now.ToString("dd/MM/yyyy HH:mm", culture);
        var totalCapital = informe.Cuotas.Sum(x => x.Amortizacion ?? 0m);
        var totalInteres = informe.Cuotas.Sum(x => x.Interes ?? 0m);
        var totalCuota = informe.Cuotas.Sum(x => x.Cuota ?? 0m);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7.2f).FontColor(Colors.Black));

                page.Header().Element(c => ComposeHeader(c, logo, fechaEmision));
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(1.02f).Element(c => ComposeClientePanel(c, cab));
                        row.ConstantItem(8);
                        row.RelativeItem(0.96f).Element(c => ComposeResumenCredito(c, cab));
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => ComposeCondicionesPanel(c, cab));
                        row.ConstantItem(8);
                        row.RelativeItem().Column(cards =>
                        {
                            cards.Spacing(7);
                            cards.Item().Row(r =>
                            {
                                r.RelativeItem().Element(c => KpiCard(c, IconKind.Money, "Monto desembolsado", cab.Desembolso));
                                r.ConstantItem(5);
                                r.RelativeItem().Element(c => KpiCard(c, IconKind.Briefcase, "Cuota a pagar", cab.CuotaReferencial));
                            });
                            cards.Item().Row(r =>
                            {
                                r.RelativeItem().Element(c => KpiCard(c, IconKind.Card, "Total a pagar", cab.TotalDevolver));
                                r.ConstantItem(5);
                                r.RelativeItem().Element(c => KpiCard(c, IconKind.Percent, "Interés total", cab.InteresesTotales));
                            });
                        });
                    });

                    col.Item().Element(c => ComposeCronograma(c, informe.Cuotas, totalCapital, totalInteres, totalCuota, culture, inv));

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(ComposeObservacion);
                        row.ConstantItem(12);
                        row.RelativeItem().Element(ComposeDeclaracionCliente);
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => ComposeFirma(c, IconKind.SignatureClient, "Firma del Cliente"));
                        row.ConstantItem(28);
                        row.RelativeItem().Element(c => ComposeFirma(c, IconKind.SignatureAdvisor, "Firma del Asesor de Créditos"));
                    });
                });

                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, byte[] logo, string fechaEmision)
    {
        container.Row(row =>
        {
            row.ConstantItem(112).Height(52).PaddingRight(8).Image(logo).FitArea();
            row.RelativeItem().Background(Brand).PaddingVertical(11).AlignMiddle().AlignCenter()
                .Text("PLAN DE PAGOS")
                .Bold()
                .FontSize(18)
                .FontColor(Colors.White);
            row.ConstantItem(108).Background(Brand).Padding(7).AlignMiddle().AlignRight().Text(t =>
            {
                t.Span("Fecha de emisión:\n").SemiBold().FontSize(5.8f).FontColor(Colors.White);
                t.Span(fechaEmision).FontSize(6.4f).FontColor(Colors.White);
            });
        });
    }

    private static void ComposeClientePanel(IContainer container, RptSimuladorPlanPagosCabeceraDto cab) =>
        Panel(container, "DATOS DEL CLIENTE", IconKind.Client, col =>
        {
            Field(col, "Cliente", cab.Cliente, boldValue: true);
            Field(col, cab.TipoDocumento, cab.NroDocumento);
            Field(col, "Dirección cliente", cab.DireccionCliente);
            Field(col, "Dirección negocio", cab.DireccionNegocio);
            Field(col, "Producto", cab.Producto, boldValue: true);
            Field(col, "Asesor", cab.Asesor);
            Field(col, "Teléfono", cab.TelefonoCliente);
        });

    private static void ComposeResumenCredito(IContainer container, RptSimuladorPlanPagosCabeceraDto cab) =>
        Panel(container, "RESUMEN DEL CRÉDITO", IconKind.Money, col =>
        {
            SummaryRow(col, "Monto solicitado", cab.Monto, highlight: true);
            SummaryRow(col, "Gastos administrativos", cab.GastosAdm);
            SummaryRow(col, "Monto desembolsado", cab.Desembolso, highlight: true);
            SummaryRow(col, "Intereses totales", cab.InteresesTotales);
            SummaryRow(col, "Total a devolver", cab.TotalDevolver, highlight: true);
            SummaryRow(col, "Cuota a pagar", cab.CuotaReferencial);
            SummaryRow(col, "Número de cuotas", cab.Cuotas);
        });

    private static void ComposeCondicionesPanel(IContainer container, RptSimuladorPlanPagosCabeceraDto cab) =>
        Panel(container, "CONDICIONES DEL CRÉDITO", IconKind.Conditions, col =>
        {
            Field(col, "Modalidad", cab.Modalidad, boldValue: true);
            Field(col, "TEM", cab.Tem);
            Field(col, "Garantía", cab.PrendaDescripcion);
            Field(col, "Primer pago", cab.Fecha, boldValue: true);
            Field(col, "Último pago", cab.FechaUltimoPago, boldValue: true);
        });

    private static void KpiCard(IContainer container, IconKind icon, string label, string value)
    {
        container
            .MinHeight(63)
            .Border(0.9f)
            .BorderColor(Border)
            .Background(Colors.White)
            .Padding(7)
            .Column(col =>
            {
                col.Spacing(3);
                col.Item().AlignCenter().Width(15).Height(15).Svg(IconSvg(icon, "#004784")).FitArea();
                col.Item().AlignCenter().Text(label.ToUpperInvariant()).Bold().FontSize(5.5f).FontColor(Brand);
                col.Item().AlignCenter().Text(value).Bold().FontSize(8).FontColor(BrandDark);
            });
    }

    private static void ComposeCronograma(
        IContainer container,
        IReadOnlyList<SimuladorCreditoCuotaDto> cuotas,
        decimal totalCapital,
        decimal totalInteres,
        decimal totalCuota,
        CultureInfo culture,
        CultureInfo inv)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionTitle(c, "CRONOGRAMA DE PAGOS"));
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(35);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                foreach (var header in new[] { "N°", "FECHA DE PAGO", "CAPITAL (S/)", "INTERÉS (S/)", "CUOTA (S/)", "SALDO (S/)" })
                {
                    table.Cell().Element(TableHeaderCell).Text(header).Bold().FontSize(6).FontColor(BrandDark);
                }

                for (var i = 0; i < cuotas.Count; i++)
                {
                    var cu = cuotas[i];
                    var bg = i % 2 == 0 ? Colors.White : Zebra;
                    TableCell(table, cu.Numero?.ToString(inv) ?? string.Empty, bg, center: true);
                    TableCell(table, cu.FechaPago?.ToString("dd/MM/yyyy", culture) ?? string.Empty, bg, center: true);
                    TableCell(table, Money(cu.Amortizacion, culture), bg, right: true);
                    TableCell(table, Money(cu.Interes, culture), bg, right: true);
                    TableCell(table, Money(cu.Cuota, culture), bg, right: true);
                    TableCell(table, Money(cu.Saldo, culture), bg, right: true);
                }

                table.Cell().ColumnSpan(2).Element(TotalCell).Text("TOTALES").Bold().FontSize(6).FontColor(Brand);
                table.Cell().Element(TotalCellRight).Text(totalCapital.ToString("N2", culture)).Bold().FontSize(6).FontColor(Brand);
                table.Cell().Element(TotalCellRight).Text(totalInteres.ToString("N2", culture)).Bold().FontSize(6).FontColor(Brand);
                table.Cell().Element(TotalCellRight).Text(totalCuota.ToString("N2", culture)).Bold().FontSize(6).FontColor(Brand);
                table.Cell().Element(TotalCellRight).Text(string.Empty);
            });
        });
    }

    private static void ComposeObservacion(IContainer container) =>
        Notice(container, IconKind.Alert, "OBSERVACIÓN",
            "El atraso en el pago genera intereses moratorios según reglamento interno de Crediconfiable.\n\nEste plan de pagos está sujeto a evaluación y aprobación final.");

    private static void ComposeDeclaracionCliente(IContainer container) =>
        Notice(container, null, "DECLARACIÓN DEL CLIENTE",
            "Declaro haber recibido información clara y suficiente sobre las condiciones del crédito, cronograma de pagos, intereses, gastos administrativos y consecuencias del incumplimiento.");

    private static void ComposeFirma(IContainer container, IconKind icon, string title)
    {
        container
            .MinHeight(82)
            .Border(0.7f)
            .BorderColor(Border)
            .Padding(12)
            .Column(col =>
            {
                col.Item().Width(16).Height(16).Svg(IconSvg(icon, "#004784")).FitArea();
                col.Item().PaddingTop(14).AlignCenter().LineHorizontal(0.7f).LineColor(Colors.Grey.Darken1);
                col.Item().AlignCenter().Text(title).FontSize(6.3f);
                col.Item().PaddingTop(12).Text("DNI:________________________").FontSize(6.5f);
            });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Background(Brand).PaddingVertical(7).PaddingHorizontal(10).Row(row =>
        {
            row.RelativeItem().Text("INVERSIONES CREDICONFIABLE S.A.C.\nAyacucho - Huanta - Andahuaylas")
                .Bold()
                .FontSize(6.5f)
                .FontColor(Colors.White);
            row.RelativeItem().AlignCenter().Text("Tel. 954 892 020")
                .FontSize(6.5f)
                .FontColor(Colors.White);
            row.RelativeItem().AlignRight().Text("crediconfiable280@gmail.com")
                .FontSize(6.5f)
                .FontColor(Colors.White);
        });
    }

    private static void Panel(IContainer container, string title, IconKind icon, Action<ColumnDescriptor> content)
    {
        container.Border(0.7f).BorderColor(Border).Column(col =>
        {
            col.Item().Element(c => SectionTitle(c, title, icon));
            col.Item().Padding(7).Column(content);
        });
    }

    private static void SectionTitle(IContainer container, string title, IconKind? icon = null)
    {
        container.Background(Brand).PaddingVertical(4).PaddingHorizontal(8).Row(row =>
        {
            if (icon.HasValue)
            {
                row.ConstantItem(18).Width(12).Height(12).Svg(IconSvg(icon.Value, "#FFFFFF")).FitArea();
            }

            row.RelativeItem().AlignMiddle().Text(title)
                .Bold()
                .FontSize(7)
                .FontColor(Colors.White);
        });
    }

    private static void Field(ColumnDescriptor col, string label, string value, bool boldValue = false)
    {
        col.Item().PaddingBottom(2).Row(row =>
        {
            row.ConstantItem(74).Text($"{label}:").SemiBold().FontSize(6.2f).FontColor(Colors.Black);
            var text = row.RelativeItem().Text(value);
            if (boldValue)
            {
                text.Bold();
            }
            text.FontSize(6.2f);
        });
    }

    private static void SummaryRow(ColumnDescriptor col, string label, string value, bool highlight = false)
    {
        col.Item().BorderBottom(0.35f).BorderColor(Border).PaddingVertical(3).Row(row =>
        {
            row.RelativeItem().Text(label).SemiBold().FontSize(6).FontColor(highlight ? Brand : Colors.Black);
            row.ConstantItem(82).AlignRight().Text(value).Bold().FontSize(6).FontColor(highlight ? Brand : Colors.Black);
        });
    }

    private static void Notice(IContainer container, IconKind? icon, string title, string text)
    {
        container.Border(0.7f).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().Row(row =>
            {
                if (icon.HasValue)
                {
                    row.ConstantItem(18).Width(13).Height(13).Svg(IconSvg(icon.Value, "#004784")).FitArea();
                }
                row.RelativeItem().Text(title).Bold().FontSize(7).FontColor(Brand);
            });
            col.Item().PaddingTop(6).Text(text).Italic().FontSize(5.8f);
        });
    }

    private static IContainer TableHeaderCell(IContainer c) =>
        c.Background(BrandSoft).Border(0.45f).BorderColor(Border).Padding(3).AlignCenter().AlignMiddle();

    private static IContainer BodyCell(IContainer c, Color bg) =>
        c.Background(bg).Border(0.35f).BorderColor(Border).PaddingVertical(3).PaddingHorizontal(4).AlignMiddle();

    private static IContainer TotalCell(IContainer c) =>
        c.BorderTop(0.6f).BorderColor(Border).Padding(3).AlignCenter().AlignMiddle();

    private static IContainer TotalCellRight(IContainer c) =>
        TotalCell(c).AlignRight();

    private static void TableCell(TableDescriptor table, string value, Color bg, bool right = false, bool center = false)
    {
        var cell = table.Cell().Element(c => BodyCell(c, bg));
        if (right)
        {
            cell = cell.AlignRight();
        }
        else if (center)
        {
            cell = cell.AlignCenter();
        }

        cell.Text(value).FontSize(6);
    }

    private static string Money(decimal? value, CultureInfo culture) =>
        value.HasValue ? value.Value.ToString("N2", culture) : string.Empty;

    private static string IconSvg(IconKind kind, string color)
    {
        var body = kind switch
        {
            IconKind.Client => """
                <circle cx="12" cy="7.5" r="3.5"/>
                <path d="M4.5 21c.8-4.4 4-7 7.5-7s6.7 2.6 7.5 7"/>
                """,
            IconKind.Money => """
                <path d="M12 3v18"/>
                <path d="M17 7.5c-1.2-1-2.7-1.5-4.5-1.5-2.5 0-4.2 1.1-4.2 2.8 0 4.2 9.4 1.8 9.4 6.4 0 1.9-1.9 3.1-4.9 3.1-2 0-3.8-.6-5.2-1.8"/>
                """,
            IconKind.Conditions => """
                <path d="M10.3 3h3.4l.6 2.4 2.2.9 2.1-1.3 2.4 2.4-1.3 2.1.9 2.2 2.4.6v3.4l-2.4.6-.9 2.2 1.3 2.1-2.4 2.4-2.1-1.3-2.2.9-.6 2.4h-3.4l-.6-2.4-2.2-.9-2.1 1.3L3 20.6l1.3-2.1-.9-2.2-2.4-.6v-3.4l2.4-.6.9-2.2L3 7.4 5.4 5l2.1 1.3 2.2-.9z"/>
                <circle cx="12" cy="12" r="3.2"/>
                """,
            IconKind.Calendar => """
                <rect x="4" y="5" width="16" height="15" rx="2"/>
                <path d="M8 3v4M16 3v4M4 9h16"/>
                """,
            IconKind.Briefcase => """
                <rect x="4" y="7" width="16" height="12" rx="2"/>
                <path d="M9 7V5h6v2M4 12h16M10 12v2h4v-2"/>
                """,
            IconKind.Card => """
                <rect x="3.5" y="6" width="17" height="12" rx="2"/>
                <path d="M3.5 9h17M7 14h4"/>
                """,
            IconKind.Percent => """
                <path d="M18 6 6 18"/>
                <circle cx="7" cy="7" r="2"/>
                <circle cx="17" cy="17" r="2"/>
                """,
            IconKind.Alert => """
                <circle cx="12" cy="12" r="9"/>
                <path d="M12 7v6M12 16.5v.1"/>
                """,
            IconKind.SignatureClient => """
                <circle cx="9" cy="7" r="3"/>
                <path d="M3.5 18c.7-3.7 2.9-5.7 5.5-5.7 1.5 0 2.9.7 3.9 2"/>
                <path d="M15 17.2l1.8 1.8 4-5"/>
                """,
            IconKind.SignatureAdvisor => """
                <circle cx="12" cy="6.5" r="3"/>
                <path d="M5 20c.8-4.4 3.7-6.6 7-6.6s6.2 2.2 7 6.6"/>
                <path d="M9 12.8 12 16l3-3.2"/>
                """,
            _ => "",
        };

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="{color}" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              {body}
            </svg>
            """;
    }
}
