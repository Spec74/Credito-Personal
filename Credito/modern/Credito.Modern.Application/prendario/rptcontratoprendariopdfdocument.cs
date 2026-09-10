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
            PrendarioPdfLayout.Encabezado(col, "ANEXO A", d.NumeroContrato, "HOJA RESUMEN");
            PrendarioPdfLayout.EncabezadoFecha(
                col,
                PrendarioPdfTexto.FechaCorta(d.FechaEmision),
                d.PlazoTexto,
                PrendarioPdfTexto.FechaCorta(d.FechaVencimiento));

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

            col.Item().PaddingTop(6).Text(
                    "GRUPO CREDICONFIANZA S.A.C. recibió del CLIENTE en garantía prendaria del préstamo que le ha otorgado los bienes detallados a continuación, de acuerdo a las condiciones establecidas en los anexos y en el contrato adjunto.")
                .FontSize(8);

            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.15f);
                    c.RelativeColumn(3.05f);
                    c.RelativeColumn(1.15f);
                    c.RelativeColumn(1.15f);
                });
                table.Cell().ColumnSpan(2).Element(c => PrendarioPdfLayout.Banda(c, "DETALLE DE LA PRENDA AFECTADA"));
                table.Cell().Element(c => PrendarioPdfLayout.Banda(c, "TASACIÓN"));
                table.Cell().Element(c => PrendarioPdfLayout.Banda(c, "PRÉSTAMO"));

                var filas = d.Bienes.Count * 7;
                var primera = true;
                foreach (var bien in d.Bienes)
                {
                    var codigo = string.IsNullOrWhiteSpace(bien.CodigoInterno) ? "A/A" : bien.CodigoInterno;
                    FilaPrenda(table, "PRENDA", codigo, primera, filas, d);
                    primera = false;
                    FilaPrenda(table, "DESCRIPCIÓN", bien.Descripcion, false, filas, d, 28);
                    FilaPrenda(table, "MARCA", PrendarioPdfTexto.Valor(bien.Marca), false, filas, d);
                    FilaPrenda(table, "MODELO", PrendarioPdfTexto.Valor(bien.Modelo), false, filas, d);
                    FilaPrenda(table, "SERIE", bien.Serie, false, filas, d);
                    FilaPrenda(table, "COLOR", PrendarioPdfTexto.Valor(bien.Color), false, filas, d);
                    FilaPrenda(table, "OBSERVACIONES", PrendarioPdfTexto.Valor(bien.Observaciones), false, filas, d, 22);
                }

                table.Cell().ColumnSpan(2).Element(c => PrendarioPdfLayout.Banda(c, "TOTAL"));
                table.Cell().Element(c => PrendarioPdfLayout.MontoTotal(c, d.MontoTasacion));
                table.Cell().Element(c => PrendarioPdfLayout.MontoTotal(c, d.MontoPrestamo));
            });

            PrendarioPdfLayout.Firmas(col);
        });
    }

    private static void FilaPrenda(
        TableDescriptor table,
        string label,
        string valor,
        bool conMontos,
        int filas,
        PrendarioContratoDto d,
        int minHeight = 16)
    {
        PrendarioPdfLayout.FilaPrenda(table, label, valor, minHeight);
        if (conMontos)
        {
            table.Cell().RowSpan((uint)filas).Element(c => PrendarioPdfLayout.MontoPrenda(c, d.MontoTasacion));
            table.Cell().RowSpan((uint)filas).Element(c => PrendarioPdfLayout.MontoPrenda(c, d.MontoPrestamo));
        }
    }

    private static void ComposeAnexoB(PageDescriptor page, PrendarioContratoDto d)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(18);
        page.MarginVertical(16);
        page.DefaultTextStyle(x => x.FontSize(9));

        var plazo = PrendarioPdfTexto.PartesPlazo(d.PlazoTexto);

        page.Content().Column(col =>
        {
            PrendarioPdfLayout.Encabezado(col, "ANEXO B", d.NumeroContrato, "HOJA RESUMEN");

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
                PrendarioPdfLayout.FilaMonto(table, "PLAZO", plazo.Numero, "MESES");
                PrendarioPdfLayout.FilaMonto(table, "CUOTAS", "1", "CUOTAS");
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "TASA DE INTERÉS COMPENSATORIA EFECTIVA MENSUAL",
                    PrendarioPdfTexto.PorcentajeFijo(d.TasaInteres),
                    PrendarioPdfTexto.PorcentajeMensualEnLetras(d.TasaInteres));
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "R. G. ADM.",
                    PrendarioPdfTexto.PorcentajeFijo(PrendarioPdfTexto.RgAdmPorcentaje),
                    "(Uno porciento del monto de préstamo)");
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "IGV",
                    PrendarioPdfTexto.PorcentajeEntero(PrendarioPdfTexto.IgvPorcentaje),
                    "Dieciocho por ciento");
                PrendarioPdfLayout.FilaMonto(
                    table,
                    "COMISIÓN DE VENTA",
                    PrendarioPdfTexto.PorcentajeEntero(PrendarioPdfTexto.ComisionVentaPorcentaje),
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
}
