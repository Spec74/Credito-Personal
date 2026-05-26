using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Plan simulador (paridad <c>Reporte/ReporteSimuladorPlanPagos</c> RDLC).</summary>
public static class RptSimuladorPlanPagosPdfDocument
{
    static RptSimuladorPlanPagosPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(RptSimuladorPlanPagosInformeDto informe)
    {
        var cab = informe.Cabecera;
        var inv = CultureInfo.InvariantCulture;
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().AlignCenter().Text("PLAN DE PAGOS — SIMULADOR").Bold().FontSize(13);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Cliente: {cab.Cliente}");
                        r.RelativeItem().Text($"Producto: {cab.Producto}");
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Monto: {cab.Monto}");
                        r.RelativeItem().Text($"Cuotas: {cab.Cuotas}");
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Modalidad: {cab.Modalidad}");
                        r.RelativeItem().Text($"1er pago: {cab.Fecha}");
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"TEM: {cab.Tem}");
                        r.RelativeItem().Text($"Desembolso: {cab.Desembolso}");
                    });
                    col.Item().Text($"Gastos adm.: {cab.GastosAdm}");

                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(32);
                            c.RelativeColumn();
                            c.ConstantColumn(72);
                            c.ConstantColumn(58);
                            c.ConstantColumn(58);
                            c.ConstantColumn(52);
                            c.ConstantColumn(58);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(H).Text("N°");
                            h.Cell().Element(H).AlignRight().Text("Capital");
                            h.Cell().Element(H).Text("Fecha");
                            h.Cell().Element(H).AlignRight().Text("Amort.");
                            h.Cell().Element(H).AlignRight().Text("Interés");
                            h.Cell().Element(H).AlignRight().Text("G.Adm");
                            h.Cell().Element(H).AlignRight().Text("Cuota");
                        });
                        foreach (var cu in informe.Cuotas)
                        {
                            table.Cell().Element(B).Text(cu.Numero?.ToString(inv) ?? "");
                            table.Cell().Element(B).AlignRight().Text((cu.Capital ?? 0).ToString("N2", inv));
                            table.Cell().Element(B).Text(cu.FechaPago?.ToString("dd/MM/yyyy", inv) ?? "");
                            table.Cell().Element(B).AlignRight().Text((cu.Amortizacion ?? 0).ToString("N2", inv));
                            table.Cell().Element(B).AlignRight().Text((cu.Interes ?? 0).ToString("N2", inv));
                            table.Cell().Element(B).AlignRight().Text((cu.GastosAdm ?? 0).ToString("N2", inv));
                            table.Cell().Element(B).AlignRight().Text((cu.Cuota ?? 0).ToString("N2", inv));
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    private static IContainer H(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold().FontSize(8)).Padding(3).Background(Colors.Grey.Lighten3);

    private static IContainer B(IContainer c) => c.Padding(3).BorderBottom(0.25f).BorderColor(Colors.Grey.Lighten3);
}
