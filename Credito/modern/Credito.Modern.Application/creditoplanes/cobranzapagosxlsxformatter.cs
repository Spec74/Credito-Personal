using System.Globalization;
using ClosedXML.Excel;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Excel real (.xlsx) — evita aviso de extensión en Excel moderno (HTML disfrazado de .xls).
/// Misma información que <see cref="CobranzaPagosExcelHtmlFormatter"/>.
/// </summary>
public static class CobranzaPagosXlsxFormatter
{
    private static readonly XLColor HeaderBg = XLColor.FromHtml("#343A40");
    private static readonly XLColor HeaderFg = XLColor.White;
    private static readonly XLColor PagoOk = XLColor.FromHtml("#17A2B8");
    private static readonly XLColor PagoFail = XLColor.FromHtml("#DC3545");
    private static readonly XLColor ClienteFg = XLColor.FromHtml("#0066CC");

    public static byte[] ToXlsx(
        IReadOnlyList<RptCobroDiarioDetalleRowDto> rows,
        string gestorNombre,
        string oficinaNombre,
        DateTime fechaImpresion)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Cobranza");
        var inv = CultureInfo.InvariantCulture;
        var r = 1;

        ws.Cell(r, 1).Value = "REPORTE DE COBRANZA POR GESTOR";
        ws.Range(r, 1, r, 10).Merge().Style
            .Font.SetBold(true).Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        r++;

        ws.Cell(r, 1).Value =
            $"Oficina: {oficinaNombre} | Gestor: {gestorNombre} | Fecha: {fechaImpresion:dd/MM/yyyy HH:mm}";
        ws.Range(r, 1, r, 10).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        r += 2;

        var headerRow = r;
        var headers = new[]
        {
            "Nro", "Cliente", "Tipo", "Crédito", "Interés", "Monto Total", "1er Pago", "Vencimiento",
            "Total Pagado", "Saldo",
        };
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(r, c + 1);
            cell.Value = headers[c];
            StyleHeader(cell);
        }

        r++;

        foreach (var item in rows)
        {
            WriteClienteRow(ws, ref r, item, inv);
            WritePagosRows(ws, ref r, item, fechaImpresion, inv);
        }

        ws.Cell(r, 1).Value = $"TOTALES ({rows.Count} clientes):";
        ws.Range(r, 1, r, 3).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        StyleTotals(ws.Cell(r, 1));
        ws.Cell(r, 4).Value = rows.Sum(x => x.MontoCredito);
        ws.Cell(r, 5).Value = rows.Sum(x => x.Interes);
        ws.Cell(r, 6).Value = rows.Sum(x => x.MontoTotal ?? 0);
        ws.Cell(r, 9).Value = rows.Sum(x => x.TotalPago ?? 0);
        ws.Cell(r, 10).Value = rows.Sum(x => x.Saldo ?? 0);
        for (var c = 4; c <= 10; c++)
        {
            if (c is 7 or 8) continue;
            ws.Cell(r, c).Style.NumberFormat.Format = "#,##0.00";
            StyleTotals(ws.Cell(r, c));
        }

        ws.Columns(1, 10).AdjustToContents(2, 45);
        ws.SheetView.FreezeRows(headerRow);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms.ToArray();
    }

    private static void WriteClienteRow(IXLWorksheet ws, ref int r, RptCobroDiarioDetalleRowDto item, CultureInfo inv)
    {
        if (item.Nro is > 0)
        {
            ws.Cell(r, 1).Value = item.Nro.Value;
        }
        ws.Cell(r, 2).Value = item.Cliente ?? "";
        ws.Cell(r, 2).Style.Font.FontColor = ClienteFg;
        ws.Cell(r, 2).Style.Font.Bold = true;
        ws.Cell(r, 3).Value = item.FormaPago;
        ws.Cell(r, 4).Value = item.MontoCredito;
        ws.Cell(r, 5).Value = item.Interes;
        ws.Cell(r, 6).Value = item.MontoTotal ?? 0;
        ws.Cell(r, 7).Value = item.FechaPrimerPago;
        ws.Cell(r, 7).Style.DateFormat.Format = "d/MM/yyyy";
        ws.Cell(r, 8).Value = item.FechaVencimiento;
        ws.Cell(r, 8).Style.DateFormat.Format = "d/MM/yyyy";
        ws.Cell(r, 9).Value = item.TotalPago ?? 0;
        ws.Cell(r, 10).Value = item.Saldo ?? 0;
        for (var c = 4; c <= 6; c++)
        {
            ws.Cell(r, c).Style.NumberFormat.Format = "#,##0.00";
        }

        for (var c = 9; c <= 10; c++)
        {
            ws.Cell(r, c).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(r, c).Style.Font.Bold = true;
        }

        r++;
    }

    private static void WritePagosRows(
        IXLWorksheet ws,
        ref int r,
        RptCobroDiarioDetalleRowDto item,
        DateTime fechaImpresion,
        CultureInfo inv)
    {
        var pagos = CobranzaPagosCuotasParser.Parse(item.Pagos);
        if (pagos.Count == 0)
        {
            return;
        }

        for (var rowStart = 0; rowStart < pagos.Count; rowStart += CobranzaPagosCuotasParser.PagosPerRow)
        {
            for (var pass = 0; pass < 2; pass++)
            {
                for (var col = 0; col < CobranzaPagosCuotasParser.PagosPerRow; col++)
                {
                    var idx = rowStart + col;
                    var cellCol = 3 + col;
                    if (idx >= pagos.Count)
                    {
                        continue;
                    }

                    var p = pagos[idx];
                    var cell = ws.Cell(r, cellCol);
                    cell.Value = pass == 0 ? p.Monto : p.Fecha;
                    cell.Style.Fill.BackgroundColor = p.EsCero ? PagoFail : PagoOk;
                    cell.Style.Font.FontColor = HeaderFg;
                    cell.Style.Font.Bold = pass == 0;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                r++;
            }
        }

        var (pagosCount, impagosCount) = CobranzaPagosCuotasParser.Count(pagos);
        var diasAtraso = Math.Max(0, (fechaImpresion.Date - item.FechaVencimiento.Date).Days);
        ws.Cell(r, 1).Value = $"Pagos: {pagosCount}  Impagos: {impagosCount}  días atraso: {diasAtraso}";
        ws.Range(r, 1, r, 2).Merge().Style.Font.Bold = true;
        r++;
    }

    private static void StyleHeader(IXLCell cell)
    {
        cell.Style.Fill.BackgroundColor = HeaderBg;
        cell.Style.Font.FontColor = HeaderFg;
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    private static void StyleTotals(IXLCell cell)
    {
        cell.Style.Fill.BackgroundColor = HeaderBg;
        cell.Style.Font.FontColor = HeaderFg;
        cell.Style.Font.Bold = true;
    }
}
