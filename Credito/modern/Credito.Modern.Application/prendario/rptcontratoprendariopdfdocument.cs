using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Prendario;

/// <summary>Anexo A + Anexo B, layout del RDLC impreso.</summary>
public static class RptContratoPrendarioPdfDocument
{
    static RptContratoPrendarioPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(PrendarioContratoDto d)
    {
        var anexos = Document.Create(document =>
        {
            document.Page(page => ComposeAnexoA(page, d));
            document.Page(page => ComposeAnexoB(page, d));
        }).GeneratePdf();

        return PrendarioPdfMerge.ConClausulas(anexos, d.FechaEmision, d.ApellidosNombres, d.DniCliente);
    }

    private static void ComposeAnexoA(PageDescriptor page, PrendarioContratoDto d)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(18);
        page.MarginVertical(16);
        page.DefaultTextStyle(x => x.FontSize(9));

        page.Content().Column(col =>
        {
            PrendarioPdfLayout.Encabezado(col, "ANEXO A - HOJA RESUMEN", d.NumeroContrato);
            col.Item().PaddingTop(8).Table(table =>
                PrendarioPdfLayout.EncabezadoFecha(
                    table,
                    PrendarioPdfTexto.FechaCorta(d.FechaEmision),
                    d.PlazoTexto,
                    PrendarioPdfTexto.FechaCorta(d.FechaVencimiento)));

            col.Item().PaddingTop(8);
            PrendarioPdfLayout.Par(
                col,
                "APELLIDOS Y NOMBRES",
                d.ApellidosNombres,
                "DNI N°",
                d.DniCliente);
            PrendarioPdfLayout.Par(
                col,
                "CONYUGE",
                PrendarioPdfTexto.Valor(d.ConyugeNombre),
                "DNI N°",
                PrendarioPdfTexto.Valor(d.ConyugeDni));
            PrendarioPdfLayout.Par(
                col,
                "CORREO",
                PrendarioPdfTexto.Valor(d.Correo),
                "CELULAR",
                PrendarioPdfTexto.Valor(d.Celular));
            PrendarioPdfLayout.Par(
                col,
                "DOMICILIO",
                PrendarioPdfTexto.Valor(d.Domicilio),
                "DISTRITO",
                PrendarioPdfTexto.Valor(d.Distrito));
            PrendarioPdfLayout.Entero(col, "REFERENCIA", PrendarioPdfTexto.Valor(d.Referencia));

            col.Item().PaddingTop(8).Text(
                    "GRUPO CREDICONFIANZA S.A.C. recibió del CLIENTE en garantía prendaria del préstamo que le ha otorgado los bienes detallados a continuación, de acuerdo a las condiciones establecidas en los anexos y en el contrato adjunto.")
                .FontSize(9);

            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(4.2f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.2f);
                });
                table.Cell().Element(c => PrendarioPdfLayout.Banda(c, "DETALLE DE LA PRENDA AFECTADA"));
                table.Cell().Element(c => PrendarioPdfLayout.Banda(c, "TASACIÓN"));
                table.Cell().Element(c => PrendarioPdfLayout.Banda(c, "PRÉSTAMO"));

                table.Cell().Element(c =>
                    c.Border(0.6f).BorderColor(Colors.Black).Padding(4).Column(detalle =>
                    {
                        var i = 1;
                        foreach (var bien in d.Bienes)
                        {
                            if (d.Bienes.Count > 1)
                            {
                                detalle.Item().Text("PRENDA " + i.ToString(System.Globalization.CultureInfo.InvariantCulture))
                                    .SemiBold().FontSize(8);
                            }
                            else
                            {
                                Linea(detalle, "PRENDA", string.IsNullOrWhiteSpace(bien.CodigoInterno) ? "A/A" : bien.CodigoInterno);
                            }

                            Linea(detalle, "DESCRIPCIÓN", bien.Descripcion);
                            Linea(detalle, "MARCA", PrendarioPdfTexto.Valor(bien.Marca));
                            Linea(detalle, "MODELO", PrendarioPdfTexto.Valor(bien.Modelo));
                            Linea(detalle, "SERIE", bien.Serie);
                            Linea(detalle, "COLOR", PrendarioPdfTexto.Valor(bien.Color));
                            Linea(detalle, "OBSERVACIONES", PrendarioPdfTexto.Valor(bien.Observaciones));
                            i++;
                        }
                    }));
                table.Cell().Element(c =>
                    c.Border(0.6f).BorderColor(Colors.Black).AlignMiddle().AlignCenter()
                        .Text(PrendarioPdfTexto.Money(d.MontoTasacion)).FontSize(10));
                table.Cell().Element(c =>
                    c.Border(0.6f).BorderColor(Colors.Black).AlignMiddle().AlignCenter()
                        .Text(PrendarioPdfTexto.Money(d.MontoPrestamo)).FontSize(10));

                table.Cell().Element(c => PrendarioPdfLayout.Banda(c, "TOTAL"));
                table.Cell().Element(c =>
                    c.Border(0.6f).BorderColor(Colors.Black).Padding(3).AlignCenter()
                        .Text(d.MontoTasacion.ToString("N2", PrendarioPdfTexto.Cultura)).FontSize(9));
                table.Cell().Element(c =>
                    c.Border(0.6f).BorderColor(Colors.Black).Padding(3).AlignCenter()
                        .Text(d.MontoPrestamo.ToString("N2", PrendarioPdfTexto.Cultura)).FontSize(9));
            });

            PrendarioPdfLayout.Firmas(col);
        });
    }

    private static void ComposeAnexoB(PageDescriptor page, PrendarioContratoDto d)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(18);
        page.MarginVertical(16);
        page.DefaultTextStyle(x => x.FontSize(9));

        page.Content().Column(col =>
        {
            PrendarioPdfLayout.Encabezado(col, "ANEXO B - HOJA RESUMEN", d.NumeroContrato);

            col.Item().PaddingTop(8).Element(c => PrendarioPdfLayout.Banda(c, "INFORMACIÓN DEL CRÉDITO"));
            col.Item().Table(table =>
            {
                PrendarioPdfLayout.ColumnasMonto(table);
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "MONTO DE TASACIÓN",
                    PrendarioPdfTexto.Money(d.MontoTasacion),
                    NumeroALetras.EnSoles(d.MontoTasacion));
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "MONTO DE PRÉSTAMO",
                    PrendarioPdfTexto.Money(d.MontoPrestamo),
                    NumeroALetras.EnSoles(d.MontoPrestamo));
                PrendarioPdfLayout.FilaMonto(table, "PLAZO", d.PlazoTexto, string.Empty);
                PrendarioPdfLayout.FilaMonto(table, "CUOTAS", "1", string.Empty);
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "TASA DE INTERÉS COMPENSATORIA EFECTIVA MENSUAL",
                    PrendarioPdfTexto.PorcentajeFijo(d.TasaInteres),
                    PrendarioPdfTexto.PorcentajeMensualEnLetras(d.TasaInteres));
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "R. G. ADM.",
                    PrendarioPdfTexto.PorcentajeFijo(PrendarioPdfTexto.RgAdmPorcentaje),
                    "(Uno por ciento del monto de préstamo)");
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "IGV",
                    PrendarioPdfTexto.PorcentajeFijo(PrendarioPdfTexto.IgvPorcentaje),
                    "Dieciocho por ciento");
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "COMISIÓN DE VENTA",
                    PrendarioPdfTexto.PorcentajeFijo(PrendarioPdfTexto.ComisionVentaPorcentaje),
                    "Cinco por ciento");
                PrendarioPdfLayout.FilaFecha(table, "FECHA DE DESEMBOLSO", PrendarioPdfTexto.FechaLargaConDia(d.FechaDesembolso));
                PrendarioPdfLayout.FilaFecha(table, "FECHA DE VENCIMIENTO", PrendarioPdfTexto.FechaLargaConDia(d.FechaVencimiento));
                PrendarioPdfLayout.FilaFecha(
                    table,
                    "FECHA DE REMATE EN CASO DE INCUMPLIMIENTO DE PAGO",
                    PrendarioPdfTexto.FechaLargaConDia(d.FechaRemate));
            });

            col.Item().PaddingTop(10).Element(c => PrendarioPdfLayout.Banda(c, "RESUMEN DEL CRÉDITO"));
            col.Item().Table(table =>
            {
                PrendarioPdfLayout.ColumnasMonto(table);
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "MONTO DE PRÉSTAMO",
                    PrendarioPdfTexto.Money(d.MontoPrestamo),
                    NumeroALetras.EnSoles(d.MontoPrestamo));
                PrendarioPdfLayout.FilaMonto(table, "CUOTA MENSUAL", "S/ -", string.Empty);
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "INTERÉS MENSUAL",
                    PrendarioPdfTexto.Money(d.InteresMensual),
                    NumeroALetras.EnSoles(d.InteresMensual));
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "INTERÉS POR DÍA",
                    PrendarioPdfTexto.Money(d.InteresDiario),
                    NumeroALetras.EnSoles(d.InteresDiario));
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "R.G.ADM",
                    PrendarioPdfTexto.Money(d.MontoGastosAdm),
                    NumeroALetras.EnSoles(d.MontoGastosAdm));
                PrendarioPdfLayout.FilaFecha(table, "FECHA DE VENCIMIENTO", PrendarioPdfTexto.FechaLargaConDia(d.FechaVencimiento));
                PrendarioPdfLayout.FilaFecha(table, "FECHA DE LIQUIDACIÓN", PrendarioPdfTexto.FechaLargaConDia(d.FechaRemate));
            });

            col.Item().PaddingTop(10).Text(
                    "Declaro haber verificado la información consignada en este Anexo A y Anexo B; que forma parte integrante del contrato; y a la vez dejo constancia de conocer las condiciones del contrato, información contenida en cuanto al monto, plazo, interés, comisiones, gastos, ejecución y otros. Por lo cual firmo y pongo mi huella en señal de conformidad.")
                .FontSize(8)
                .Justify();

            PrendarioPdfLayout.Firmas(col);
        });
    }

    private static void Linea(ColumnDescriptor col, string label, string valor) =>
        col.Item().PaddingBottom(2).Text(text =>
        {
            text.Span(label + ": ").SemiBold().FontSize(8);
            text.Span(valor).FontSize(9);
        });
}
