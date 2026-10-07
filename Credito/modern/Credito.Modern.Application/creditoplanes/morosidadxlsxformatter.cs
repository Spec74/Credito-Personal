using System.Globalization;
using ClosedXML.Excel;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Excel de morosidad empresarial (paridad MorosidadExcelService legado, ClosedXML).
/// </summary>
public static class MorosidadXlsxFormatter
{
    private const string AzulEmpresa = "#056AA0";
    private const string AzulOscuro = "#17324D";
    private const string Rojo = "#B4232A";
    private const string Ambar = "#E57C09";
    private const string Verde = "#238A57";
    private const string Empresa = "CREDICONFIABLE";

    public static byte[] ToXlsx(
        IReadOnlyList<MorosidadEmpresaItemDto> filas,
        DateTime fechaInicio,
        DateTime fechaFin)
    {
        ArgumentNullException.ThrowIfNull(filas);

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Clientes morosos");
        ConfigurarEncabezado(hoja, filas, fechaInicio, fechaFin);
        EscribirDetalle(hoja, filas);
        ConfigurarImpresion(hoja);

        libro.Properties.Title = "Morosidad empresarial";
        libro.Properties.Subject =
            $"Primera cuota vencida {fechaInicio:dd/MM/yyyy} – {fechaFin:dd/MM/yyyy}";
        libro.Properties.Company = Empresa;
        libro.Properties.Author = Empresa;

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void ConfigurarEncabezado(
        IXLWorksheet hoja,
        IReadOnlyList<MorosidadEmpresaItemDto> filas,
        DateTime fechaInicio,
        DateTime fechaFin)
    {
        hoja.Range("A1:M1").Merge();
        hoja.Cell("A1").Value = "MOROSIDAD EMPRESARIAL";
        hoja.Cell("A1").Style
            .Font.SetBold()
            .Font.SetFontSize(18)
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(AzulEmpresa))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        hoja.Row(1).Height = 30;

        hoja.Range("A2:M2").Merge();
        hoja.Cell("A2").Value = string.Create(
            CultureInfo.InvariantCulture,
            $"Clientes cuya primera cuota vencida está entre {fechaInicio:dd/MM/yyyy} y {fechaFin:dd/MM/yyyy}. Clasificación calculada al {fechaFin:dd/MM/yyyy}.");
        hoja.Cell("A2").Style
            .Font.SetFontColor(XLColor.FromHtml(AzulOscuro))
            .Fill.SetBackgroundColor(XLColor.FromHtml("#EAF4F9"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        var totalSaldo = filas.Sum(x => x.SaldoMora);
        var totalCreditos = filas.Sum(x => x.CreditosMora);

        CrearIndicador(hoja, "A4:C4", "Clientes", filas.Count.ToString("N0", CultureInfo.InvariantCulture), AzulEmpresa);
        CrearIndicador(hoja, "D4:F4", "Créditos en mora", totalCreditos.ToString("N0", CultureInfo.InvariantCulture), AzulOscuro);
        CrearIndicador(hoja, "G4:I4", "Saldo en mora", "S/ " + totalSaldo.ToString("N2", CultureInfo.InvariantCulture), AzulEmpresa);
        CrearIndicador(
            hoja,
            "J4:M4",
            "Generado",
            DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
            AzulOscuro);

        hoja.Range("A6:M6").Merge();
        hoja.Cell("A6").Value =
            "SEMÁFORO:  ROJO = Nunca pagó     |     ÁMBAR = Dejó de pagar     |     VERDE = Paga con atraso";
        hoja.Cell("A6").Style
            .Font.SetBold()
            .Font.SetFontColor(XLColor.FromHtml(AzulOscuro))
            .Fill.SetBackgroundColor(XLColor.FromHtml("#F3F6F8"));
    }

    private static void CrearIndicador(
        IXLWorksheet hoja,
        string rango,
        string etiqueta,
        string valor,
        string color)
    {
        var bloque = hoja.Range(rango);
        bloque.Merge();
        bloque.Value = etiqueta + ":  " + valor;
        bloque.Style
            .Font.SetBold()
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(color))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        bloque.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        bloque.Style.Border.OutsideBorderColor = XLColor.White;
        hoja.Row(bloque.FirstRow().RowNumber()).Height = 24;
    }

    private static void EscribirDetalle(
        IXLWorksheet hoja,
        IReadOnlyList<MorosidadEmpresaItemDto> filas)
    {
        var encabezados = new[]
        {
            "Semáforo",
            "Situación",
            "Cliente",
            "DNI",
            "Celular",
            "Oficina",
            "Usuario analista",
            "Analista a cargo",
            "Créditos en mora",
            "Saldo en mora",
            "Primera cuota vencida",
            "Último pago",
            "Días de atraso",
        };

        const int filaEncabezado = 8;
        for (var columna = 1; columna <= encabezados.Length; columna++)
        {
            hoja.Cell(filaEncabezado, columna).Value = encabezados[columna - 1];
        }

        var rangoEncabezado = hoja.Range(filaEncabezado, 1, filaEncabezado, encabezados.Length);
        rangoEncabezado.Style
            .Font.SetBold()
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(AzulOscuro))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        rangoEncabezado.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        hoja.Row(filaEncabezado).Height = 29;

        var fila = filaEncabezado + 1;
        foreach (var item in filas)
        {
            var color = ColorSemaforo(item.CodigoClasificacion);
            hoja.Cell(fila, 1).Value = TextoSemaforo(item.CodigoClasificacion);
            hoja.Cell(fila, 2).Value = item.Clasificacion;
            hoja.Cell(fila, 3).Value = item.NombreCompleto;
            hoja.Cell(fila, 4).Value = item.NumeroDocumento;
            hoja.Cell(fila, 5).Value = item.Celular;
            hoja.Cell(fila, 6).Value = item.Oficina;
            hoja.Cell(fila, 7).Value = item.GestorUsuario;
            hoja.Cell(fila, 8).Value = item.GestorNombre;
            hoja.Cell(fila, 9).Value = item.CreditosMora;
            hoja.Cell(fila, 10).Value = item.SaldoMora;

            if (item.PrimeraCuotaVencida.HasValue)
            {
                hoja.Cell(fila, 11).Value = item.PrimeraCuotaVencida.Value;
            }

            if (item.FechaUltimoPago.HasValue)
            {
                hoja.Cell(fila, 12).Value = item.FechaUltimoPago.Value;
            }

            hoja.Cell(fila, 13).Value = item.DiasAtraso;

            hoja.Cell(fila, 1).Style
                .Font.SetBold()
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml(color))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            hoja.Cell(fila, 2).Style
                .Font.SetBold()
                .Font.SetFontColor(XLColor.FromHtml(color));

            if (fila % 2 == 0)
            {
                hoja.Range(fila, 2, fila, 13).Style.Fill
                    .SetBackgroundColor(XLColor.FromHtml("#F7F9FB"));
            }

            fila++;
        }

        var ultimaFila = Math.Max(filaEncabezado, fila - 1);
        var tabla = hoja.Range(filaEncabezado, 1, ultimaFila, encabezados.Length);
        tabla.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        tabla.Style.Border.InsideBorderColor = XLColor.FromHtml("#DCE5EC");
        tabla.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tabla.Style.Border.OutsideBorderColor = XLColor.FromHtml("#BFCED9");
        tabla.SetAutoFilter();

        if (fila > filaEncabezado + 1)
        {
            hoja.Range(filaEncabezado + 1, 10, ultimaFila, 10)
                .Style.NumberFormat.Format = "\"S/ \"#,##0.00";
            hoja.Range(filaEncabezado + 1, 11, ultimaFila, 12)
                .Style.NumberFormat.Format = "dd/mm/yyyy";
        }

        hoja.SheetView.FreezeRows(filaEncabezado);
        hoja.Columns(1, 13).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        hoja.Columns(3, 8).Style.Alignment.WrapText = true;
        hoja.Column(1).Width = 12;
        hoja.Column(2).Width = 19;
        hoja.Column(3).Width = 34;
        hoja.Column(4).Width = 14;
        hoja.Column(5).Width = 14;
        hoja.Column(6).Width = 23;
        hoja.Column(7).Width = 19;
        hoja.Column(8).Width = 32;
        hoja.Column(9).Width = 17;
        hoja.Column(10).Width = 18;
        hoja.Column(11).Width = 22;
        hoja.Column(12).Width = 16;
        hoja.Column(13).Width = 16;
    }

    private static string ColorSemaforo(string codigo) =>
        codigo switch
        {
            "NUNCA_PAGO" => Rojo,
            "DEJO_PAGAR" => Ambar,
            _ => Verde,
        };

    private static string TextoSemaforo(string codigo) =>
        codigo switch
        {
            "NUNCA_PAGO" => "ROJO",
            "DEJO_PAGAR" => "ÁMBAR",
            _ => "VERDE",
        };

    private static void ConfigurarImpresion(IXLWorksheet hoja)
    {
        hoja.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        hoja.PageSetup.FitToPages(1, 0);
        hoja.PageSetup.Margins.Top = 0.35;
        hoja.PageSetup.Margins.Bottom = 0.35;
        hoja.PageSetup.Margins.Left = 0.25;
        hoja.PageSetup.Margins.Right = 0.25;
        hoja.PageSetup.SetRowsToRepeatAtTop(1, 8);
    }
}
