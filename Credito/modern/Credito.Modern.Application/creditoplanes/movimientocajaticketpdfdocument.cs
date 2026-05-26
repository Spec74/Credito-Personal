using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Ticket estrecho (paridad layout TicketCaja RDLC, sin ReportViewer).</summary>
public static class MovimientoCajaTicketPdfDocument
{
    static MovimientoCajaTicketPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(MovimientoCajaTicketDto data)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(226, 800);
                page.MarginVertical(12);
                page.MarginHorizontal(10);
                page.DefaultTextStyle(style => style.FontSize(8));

                page.Content().Column(col =>
                {
                    col.Spacing(4);
                    col.Item().AlignCenter().Text("COMPROBANTE DE CAJA").Bold().FontSize(10);
                    col.Item().Text($"Mov. N° {data.MovimientoCajaId}");
                    col.Item().Text($"Oficina: {data.Oficina}");
                    col.Item().Text($"Fecha: {data.FechaReg:g}");
                    col.Item().Text($"Cajero: {data.User}");
                    col.Item().LineHorizontal(0.5f);
                    col.Item().Text($"Cliente: {data.Cliente}");
                    if (data.PersonaId > 0)
                    {
                        col.Item().Text($"Persona ID: {data.PersonaId}");
                    }

                    col.Item().Text($"Producto: {data.Producto}");
                    if (!string.IsNullOrWhiteSpace(data.Concepto))
                    {
                        col.Item().Text($"Concepto: {data.Concepto}").Bold();
                    }

                    if (!string.IsNullOrWhiteSpace(data.Articulo))
                    {
                        col.Item().Text(data.Articulo);
                    }

                    if (data.Layout == MovimientoCajaTicketLayout.CuotaCredito
                        || data.Layout == MovimientoCajaTicketLayout.CuotaLibre)
                    {
                        col.Item().LineHorizontal(0.5f);
                        AddMoneyLine(col, "Saldo anterior", data.SaldoAnterior, inv);
                        AddMoneyLine(col, "Pago deuda", data.PagoDeuda, inv);
                        AddMoneyLine(col, "Interés", data.Interes, inv);
                        AddMoneyLine(col, "Mora / cargo", data.MoraCargo, inv);
                        AddMoneyLine(col, "Descuento", data.Descuento, inv);
                        AddMoneyLine(col, "Importe libre ant.", data.ImporteLibreAnt, inv);
                        AddMoneyLine(col, "Importe libre", data.ImporteLibre, inv);
                        AddMoneyLine(col, "Importe pagado", data.ImportePagado ?? data.ImportePago, inv);
                        AddMoneyLine(col, "Saldo capital", data.SaldoCapital, inv);
                        if (!string.IsNullOrWhiteSpace(data.CuotasPagadas))
                        {
                            col.Item().Text($"Cuotas pagadas: {data.CuotasPagadas}");
                        }

                        if (!string.IsNullOrWhiteSpace(data.ProximaCuota))
                        {
                            col.Item().Text($"Próxima cuota: {data.ProximaCuota}");
                        }

                        if (data.CuotasAtrazadas is > 0)
                        {
                            col.Item().Text($"Cuotas atrasadas: {data.CuotasAtrazadas}");
                        }

                        AddMoneyLine(col, "Mora pendiente", data.MoraTotalPendiente, inv);
                        if (!string.IsNullOrWhiteSpace(data.EstadoCredito))
                        {
                            col.Item().Text($"Estado crédito: {data.EstadoCredito}");
                        }

                        AddMoneyLine(col, "Crédito total", data.CreditoTotal, inv);
                    }
                    else
                    {
                        col.Item().LineHorizontal(0.5f);
                        col.Item().Text($"Importe: {data.ImportePago.ToString("N2", inv)}").Bold().FontSize(10);
                    }

                    col.Item().PaddingTop(8).AlignCenter().Text("*** Gracias ***").Italic();
                });
            });
        }).GeneratePdf();
    }

    private static void AddMoneyLine(
        ColumnDescriptor col,
        string label,
        decimal? value,
        System.Globalization.CultureInfo inv)
    {
        if (value is null)
        {
            return;
        }

        col.Item().Row(row =>
        {
            row.RelativeItem().Text(label);
            row.ConstantItem(70).AlignRight().Text(value.Value.ToString("N2", inv));
        });
    }
}
