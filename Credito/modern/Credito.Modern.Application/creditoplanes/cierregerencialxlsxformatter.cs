using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace Credito.Modern.Application.CreditoPlanes
{
    /// <summary>
    /// Excel gerencial profesional (misma lógica de negocio que el legacy ClosedXML).
    /// </summary>
    public static class CierreGerencialXlsxFormatter
    {
        private const string AzulOscuro = "0F4C81";
        private const string Azul = "1F4E78";
        private const string AzulClaro = "D9EAF7";
        private const string Verde = "E2F0D9";
        private const string VerdeTexto = "1B7A3D";
        private const string Amarillo = "FFF2CC";
        private const string Rojo = "FCE4D6";
        private const string RojoTexto = "9C0006";
        private const string Gris = "E7E6E6";
        private const string GrisTexto = "5B6B75";
        private const string Naranja = "C65911";
        private const string Zebra = "F7FAFC";
        private const string EspecialSoft = "FBF3EB";
        private const string Moneda = "S/ #,##0.00;[Red](S/ #,##0.00);-";
        private const string Entero = "#,##0;[Red](#,##0);-";
        private const string Porcentaje = "0.00%;[Red](0.00%);-";
        private const string Empresa = "CREDICONFIABLE";

        public static byte[] ToXlsx(IReadOnlyList<AvanceMetaGerencialDto> filas)
                {
                    if (filas == null || filas.Count == 0)
                        throw new InvalidOperationException(
                            "No existen datos para generar el Excel gerencial.");

                    var datos = filas
                        .OrderBy(x => x.Orden ?? short.MaxValue)
                        .ThenBy(x => x.NombreCompleto)
                        .ToList();

                    using (var libro = new XLWorkbook())
                    {
                        CrearResumen(libro, datos);
                        CrearDetalle(libro, datos);
                        CrearDefiniciones(libro, datos[0]);

                        CrearLeyenda(libro);

                        libro.Properties.Title = "Cierre y metas gerenciales";
                        libro.Properties.Subject =
                            datos[0].AvanceNoOficial
                                ? "Avance mensual no oficial"
                                : "Cierre mensual oficial";
                        libro.Properties.Company = Empresa;
                        libro.Properties.Author = Empresa;
                        libro.Properties.Comments =
                            "Generado por Credito Modern. Lógica de negocio alineada al cierre gerencial en producción.";

                        using (var memoria = new MemoryStream())
                        {
                            libro.SaveAs(memoria);
                            return memoria.ToArray();
                        }
                    }
                }

                private static void CrearResumen(
                    XLWorkbook libro,
                    IReadOnlyList<AvanceMetaGerencialDto> filas)
                {
                    var hoja = libro.Worksheets.Add("Resumen gerencial");
                    hoja.TabColor = XLColor.FromHtml("#" + AzulOscuro);
                    var meta = filas[0];
                    var productivas = filas
                        .Where(x => EsTipo(x, "PRODUCTIVA"))
                        .ToList();
                    var especiales = filas
                        .Where(x => EsTipo(x, "ESPECIAL"))
                        .ToList();

                    PrepararHoja(hoja);
                    EscribirMarca(hoja, "A1:O1");
                    EscribirTitulo(
                        hoja,
                        "A2:O2",
                        "CIERRE Y METAS GERENCIALES · " + NombrePeriodo(meta.Periodo));

                    hoja.Range("A3:O3").Merge();
                    hoja.Cell("A3").Value =
                        (meta.AvanceNoOficial ? "AVANCE NO OFICIAL" : "CIERRE OFICIAL") +
                        "  ·  Calculado: " + meta.FechaCalculo.ToString("dd/MM/yyyy HH:mm:ss") +
                        "  ·  " + Empresa;
                    hoja.Range("A3:O3").Style.Fill.BackgroundColor =
                        XLColor.FromHtml("#" + (meta.AvanceNoOficial ? Naranja : VerdeTexto));
                    hoja.Range("A3:O3").Style.Font.FontColor = XLColor.White;
                    hoja.Range("A3:O3").Style.Font.Bold = true;
                    hoja.Range("A3:O3").Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    EscribirKpi(hoja, "A5:C5", "A6:C7", "Saldo total al cierre",
                        filas.Sum(x => x.CapitalActual), Moneda, AzulOscuro);
                    EscribirKpi(hoja, "D5:F5", "D6:F7", "Clientes activos",
                        filas.Sum(x => x.ClientesActivosActual), Entero, Azul);
                    EscribirKpi(hoja, "G5:I5", "G6:I7", "Vencidos actuales",
                        filas.Sum(x => x.VencidosActual), Moneda, Naranja);
            EscribirKpi(hoja, "J5:M5", "J6:M7", "Metas configuradas",
                filas.Count(x => x.MetaConfigurada) + " / " + filas.Count, string.Empty, VerdeTexto);

                    var encabezados = new[]
                    {
                        "Tipo de cartera", "Carteras", "Capital base", "Meta capital",
                        "Saldo total actual", "Clientes base", "Meta clientes", "Clientes actuales",
                        "Vencidos base comparable", "Límite vencidos", "Vencidos actuales",
                        "Meta recuperación", "Recuperación actual", "Capital productivo neto",
                        "Clientes nuevos"
                    };
                    EscribirEncabezados(hoja, 10, encabezados);

                    EscribirResumenTipo(hoja, 11, "PRODUCTIVA", productivas);
                    EscribirResumenTipo(hoja, 12, "ESPECIAL", especiales);
                    EscribirResumenTipo(hoja, 13, "TOTAL", filas);
                    AplicarTotal(hoja.Range("A13:O13"));

                    hoja.Range("C11:E13").Style.NumberFormat.Format = Moneda;
                    hoja.Range("F11:H13").Style.NumberFormat.Format = Entero;
                    hoja.Range("I11:M13").Style.NumberFormat.Format = Moneda;
                    hoja.Range("N11:N13").Style.NumberFormat.Format = Moneda;
                    hoja.Range("O11:O13").Style.NumberFormat.Format = Entero;
                    hoja.Range("A10:O13").Style.Border.BottomBorder =
                        XLBorderStyleValues.Hair;
                    hoja.Range("A10:O13").Style.Border.BottomBorderColor =
                        XLColor.FromHtml("#D9E1F2");

                    hoja.Cell("A16").Value = "Lectura del periodo";
                    hoja.Cell("A16").Style.Font.Bold = true;
                    hoja.Cell("A17").Value = "Carteras productivas";
                    hoja.Cell("B17").Value = productivas.Count;
                    hoja.Cell("A18").Value = "Carteras especiales";
                    hoja.Cell("B18").Value = especiales.Count;
                    hoja.Cell("A19").Value = "Clientes con saldo vencido";
                    hoja.Cell("B19").Value = filas.Sum(x => x.ClientesVencidosActual);
                    hoja.Cell("A20").Value = "Metas pendientes de configurar";
                    hoja.Cell("B20").Value = filas.Count(x => !x.MetaConfigurada);
                    hoja.Cell("A21").Value = "Clientes nuevos del mes";
                    hoja.Cell("B21").Value = filas.Sum(x => x.ClientesNuevosMes);
                    hoja.Range("B17:B21").Style.NumberFormat.Format = Entero;

                    hoja.Cell("D16").Value = "Criterio";
                    hoja.Cell("D16").Style.Font.Bold = true;
                    hoja.Range("D17:O17").Merge();
                    hoja.Cell("D17").Value =
                        "Capital y clientes se evalúan al cierre. Vencidos se evalúa como límite final y recuperación mensual.";
                    hoja.Range("D18:O18").Merge();
                    hoja.Cell("D18").Value =
                        "Los vencidos actuales corresponden al saldo residual de cuotas vencidas después de aplicar pagos FIFO.";
                    hoja.Range("D19:O19").Merge();
                    hoja.Cell("D19").Value =
                        "Una base comparable vacía significa que el cierre anterior todavía utilizaba la definición histórica.";
                    hoja.Range("D17:O19").Style.Font.FontColor =
                        XLColor.FromHtml("#" + GrisTexto);
                    hoja.Range("D17:O19").Style.Alignment.WrapText = true;

                    hoja.Range("D20:O20").Merge();
                    hoja.Cell("D20").Value =
                        "Capital productivo neto = saldo total menos vencidos para carteras productivas. No aplica a carteras especiales.";
                    hoja.Range("D20:O20").Style.Font.FontColor =
                        XLColor.FromHtml("#" + GrisTexto);
                    hoja.Range("D20:O20").Style.Alignment.WrapText = true;

                    hoja.Range("D21:O21").Merge();
                    hoja.Cell("D21").Value =
                        "Clientes nuevos = personas distintas registradas como clientes durante el mes, atribuidas al analista dueño del crédito.";
                    hoja.Range("D21:O21").Style.Font.FontColor =
                        XLColor.FromHtml("#" + GrisTexto);
                    hoja.Range("D21:O21").Style.Alignment.WrapText = true;

                    hoja.Columns("A:O").Width = 15;
                    hoja.Column("A").Width = 28;
                    hoja.Column("D").Width = 19;
                    EscribirPie(hoja, 23, 15);
                    ConfigurarImpresion(hoja, "Resumen gerencial · " + NombrePeriodo(meta.Periodo), fitOnePage: true);
                }

                private static void EscribirResumenTipo(
                    IXLWorksheet hoja,
                    int fila,
                    string nombre,
                    IEnumerable<AvanceMetaGerencialDto> origen)
                {
                    var datos = origen.ToList();
                    hoja.Cell(fila, 1).Value = nombre;
                    hoja.Cell(fila, 2).Value = datos.Count;
                    hoja.Cell(fila, 3).Value = SumaDecimal(datos, x => x.CapitalBase);
                    AsignarNullable(hoja.Cell(fila, 4), SumaDecimalNullable(datos, x => x.MetaCapitalCierre));
                    hoja.Cell(fila, 5).Value = datos.Sum(x => x.CapitalActual);
                    AsignarNullable(hoja.Cell(fila, 6), SumaEnteroNullable(datos, x => x.ClientesBase));
                    AsignarNullable(hoja.Cell(fila, 7), SumaEnteroNullable(datos, x => x.MetaClientesActivosCierre));
                    hoja.Cell(fila, 8).Value = datos.Sum(x => x.ClientesActivosActual);
                    AsignarNullable(hoja.Cell(fila, 9), SumaDecimalNullable(datos, x => x.VencidosBaseComparable));
                    AsignarNullable(hoja.Cell(fila, 10), SumaDecimalNullable(datos, x => x.MetaVencidosMaximoCierre));
                    hoja.Cell(fila, 11).Value = datos.Sum(x => x.VencidosActual);
                    AsignarNullable(hoja.Cell(fila, 12), SumaDecimalNullable(datos, x => x.MetaRecuperacionVencidosMes));
                    AsignarNullable(hoja.Cell(fila, 13), SumaDecimalNullable(datos, x => x.RecuperacionVencidosActual));
                    AsignarNullable(hoja.Cell(fila, 14),
                        SumaDecimalNullable(datos, CapitalProductivoNeto));
                    hoja.Cell(fila, 15).Value = datos.Sum(x => x.ClientesNuevosMes);
                }

                private static void CrearDetalle(
                    XLWorkbook libro,
                    IReadOnlyList<AvanceMetaGerencialDto> filas)
                {
                    var hoja = libro.Worksheets.Add("Detalle analistas");
                    hoja.TabColor = XLColor.FromHtml("#" + Azul);
                    PrepararHoja(hoja);
                    EscribirMarca(hoja, "A1:AP1");
                    EscribirTitulo(
                        hoja,
                        "A2:AP2",
                        "DETALLE POR ANALISTA · " + NombrePeriodo(filas[0].Periodo));

                    hoja.Range("A4:C4").Merge();
                    hoja.Cell("A4").Value = "ORGANIZACIÓN";
                    hoja.Range("D4:E4").Merge();
                    hoja.Cell("D4").Value = "CLASIFICACIÓN";
                    hoja.Range("F4:O4").Merge();
                    hoja.Cell("F4").Value = "SALDOS Y CAPITAL";
                    hoja.Range("P4:V4").Merge();
                    hoja.Cell("P4").Value = "CLIENTES ACTIVOS";
                    hoja.Range("W4:AG4").Merge();
                    hoja.Cell("W4").Value = "SALDOS VENCIDOS";
                    hoja.Range("AH4:AL4").Merge();
                    hoja.Cell("AH4").Value = "CLIENTES NUEVOS, COBRANZA Y DESEMBOLSOS";
                    hoja.Range("AM4:AP4").Merge();
                    hoja.Cell("AM4").Value = "RECUPERACIÓN DE VENCIDOS";
                    AplicarEncabezado(hoja.Range("A4:AP4"), AzulOscuro);

                    EscribirEncabezados(hoja, 5, new[]
                    {
                        "Asesor", "Supervisor", "Analista", "Mercado", "Tipo",
                        "Saldo base", "Saldo actual", "Variación vs base",
                        "Capital productivo base", "Capital productivo actual", "Variación productiva",
                        "Meta cierre", "Brecha vs meta", "Cumplimiento", "Estado meta",
                        "Base", "Actual", "Variación vs base", "Meta", "Brecha vs meta",
                        "Cumplimiento", "Estado meta",
                        "Base comparable", "Referencia histórica", "Actual", "Reducción vs base",
                        "% saldo vencido", "Clientes vencidos base", "Clientes vencidos actual",
                        "Reducción de clientes", "Límite final", "Margen", "Estado",
                        "Clientes nuevos", "Monto clientes nuevos", "Monto cobrado",
                        "Desembolsos", "N.º operaciones",
                        "Meta recuperación", "Recuperación actual", "Brecha", "Estado"
                    });

                    var fila = 6;
                    foreach (var x in filas)
                    {
                        var estadoFinal = x.EstadoRecuperacion;
                        if (!x.MetaConfigurada)
                            estadoFinal = "SIN CONFIGURAR";

                        var variacionCapital = Variacion(x.CapitalBase, x.CapitalActual);
                        var capitalProductivoBase = CapitalProductivoBase(x);
                        var capitalProductivoActual = CapitalProductivoNeto(x);
                        var variacionProductiva = Variacion(
                            capitalProductivoBase,
                            capitalProductivoActual);
                        var variacionClientes = Variacion(
                            x.ClientesBase,
                            x.ClientesActivosActual);
                        var reduccionVencidos = Reduccion(
                            x.VencidosBaseComparable,
                            x.VencidosActual);
                        var reduccionClientesVencidos = Reduccion(
                            x.ClientesVencidosBase,
                            x.ClientesVencidosActual);
                        var porcentajeSaldoVencido = x.CapitalActual <= 0m
                            ? (decimal?)null
                            : x.VencidosActual / x.CapitalActual;
                        var brechaRecuperacion = Diferencia(
                            x.RecuperacionVencidosActual,
                            x.MetaRecuperacionVencidosMes);

                        EscribirFila(hoja, fila, new object[]
                        {
                            x.Asesor, x.Supervisor, x.NombreUsuario, x.Mercado, x.TipoCartera,
                            x.CapitalBase, x.CapitalActual, variacionCapital,
                            capitalProductivoBase, capitalProductivoActual, variacionProductiva,
                            x.MetaCapitalCierre, x.DiferenciaCapital,
                            AFraccion(x.CumplimientoCapitalPct), x.EstadoCapital,
                            x.ClientesBase, x.ClientesActivosActual, variacionClientes,
                            x.MetaClientesActivosCierre, x.DiferenciaClientes,
                            AFraccion(x.CumplimientoClientesPct), x.EstadoClientes,
                            x.VencidosBaseComparable, x.VencidosBaseLegacy, x.VencidosActual,
                            reduccionVencidos, porcentajeSaldoVencido,
                            x.ClientesVencidosBase, x.ClientesVencidosActual,
                            reduccionClientesVencidos, x.MetaVencidosMaximoCierre,
                            x.MargenVencidos, x.EstadoVencidos,
                            x.ClientesNuevosMes, x.MontoClientesNuevosMes,
                            x.MontoCobradoMes, x.DesembolsosMes, x.NroOperacionesMes,
                            x.MetaRecuperacionVencidosMes, x.RecuperacionVencidosActual,
                            brechaRecuperacion, estadoFinal
                        });

                        AplicarVariacion(hoja.Cell(fila, 8), variacionCapital, true);
                        AplicarVariacion(hoja.Cell(fila, 11), variacionProductiva, true);
                        AplicarVariacion(hoja.Cell(fila, 18), variacionClientes, true);
                        AplicarVariacion(hoja.Cell(fila, 26), reduccionVencidos, true);
                        AplicarVariacion(hoja.Cell(fila, 30), reduccionClientesVencidos, true);
                        AplicarVariacion(hoja.Cell(fila, 41), brechaRecuperacion, true);
                        AplicarEstado(hoja.Cell(fila, 15), x.EstadoCapital);
                        AplicarEstado(hoja.Cell(fila, 22), x.EstadoClientes);
                        AplicarEstado(hoja.Cell(fila, 33), x.EstadoVencidos);
                        AplicarEstado(hoja.Cell(fila, 42), estadoFinal);
                        AplicarFilaDetalle(hoja, fila, EsTipo(x, "ESPECIAL"));
                        fila++;
                    }

                    var filaTotal = fila;
                    hoja.Cell(filaTotal, 1).Value = filas.Count;
                    hoja.Cell(filaTotal, 3).Value = "TOTAL";
                    AsignarNullable(hoja.Cell(filaTotal, 6), SumaDecimalNullable(filas, x => x.CapitalBase));
                    hoja.Cell(filaTotal, 7).Value = filas.Sum(x => x.CapitalActual);
                    AsignarNullable(hoja.Cell(filaTotal, 8), VariacionTotalDecimal(
                        filas, x => x.CapitalBase, x => x.CapitalActual));
                    AsignarNullable(hoja.Cell(filaTotal, 9), SumaDecimalNullable(filas, CapitalProductivoBase));
                    AsignarNullable(hoja.Cell(filaTotal, 10), SumaDecimalNullable(filas, CapitalProductivoNeto));
                    AsignarNullable(hoja.Cell(filaTotal, 11), VariacionTotalDecimal(
                        filas, CapitalProductivoBase, CapitalProductivoNeto));
                    AsignarNullable(hoja.Cell(filaTotal, 12), SumaDecimalNullable(filas, x => x.MetaCapitalCierre));
                    AsignarNullable(hoja.Cell(filaTotal, 13), SumaDecimalNullable(filas, x => x.DiferenciaCapital));
                    AsignarNullable(hoja.Cell(filaTotal, 16), SumaEnteroNullable(filas, x => x.ClientesBase));
                    hoja.Cell(filaTotal, 17).Value = filas.Sum(x => x.ClientesActivosActual);
                    AsignarNullable(hoja.Cell(filaTotal, 18), VariacionTotalEntero(
                        filas, x => x.ClientesBase, x => x.ClientesActivosActual));
                    AsignarNullable(hoja.Cell(filaTotal, 19), SumaEnteroNullable(filas, x => x.MetaClientesActivosCierre));
                    AsignarNullable(hoja.Cell(filaTotal, 20), SumaEnteroNullable(filas, x => x.DiferenciaClientes));
                    AsignarNullable(hoja.Cell(filaTotal, 23), SumaDecimalNullable(filas, x => x.VencidosBaseComparable));
                    AsignarNullable(hoja.Cell(filaTotal, 24), SumaDecimalNullable(filas, x => x.VencidosBaseLegacy));
                    hoja.Cell(filaTotal, 25).Value = filas.Sum(x => x.VencidosActual);
                    AsignarNullable(hoja.Cell(filaTotal, 26), ReduccionTotalDecimal(
                        filas, x => x.VencidosBaseComparable, x => x.VencidosActual));
                    AsignarNullable(hoja.Cell(filaTotal, 28), SumaEnteroNullable(filas, x => x.ClientesVencidosBase));
                    hoja.Cell(filaTotal, 29).Value = filas.Sum(x => x.ClientesVencidosActual);
                    AsignarNullable(hoja.Cell(filaTotal, 30), ReduccionTotalEntero(
                        filas, x => x.ClientesVencidosBase, x => x.ClientesVencidosActual));
                    AsignarNullable(hoja.Cell(filaTotal, 31), SumaDecimalNullable(filas, x => x.MetaVencidosMaximoCierre));
                    AsignarNullable(hoja.Cell(filaTotal, 32), SumaDecimalNullable(filas, x => x.MargenVencidos));
                    hoja.Cell(filaTotal, 34).Value = filas.Sum(x => x.ClientesNuevosMes);
                    hoja.Cell(filaTotal, 35).Value = filas.Sum(x => x.MontoClientesNuevosMes);
                    hoja.Cell(filaTotal, 36).Value = filas.Sum(x => x.MontoCobradoMes);
                    hoja.Cell(filaTotal, 37).Value = filas.Sum(x => x.DesembolsosMes);
                    hoja.Cell(filaTotal, 38).Value = filas.Sum(x => x.NroOperacionesMes);
                    AsignarNullable(hoja.Cell(filaTotal, 39), SumaDecimalNullable(filas, x => x.MetaRecuperacionVencidosMes));
                    AsignarNullable(hoja.Cell(filaTotal, 40), SumaDecimalNullable(filas, x => x.RecuperacionVencidosActual));
                    AsignarNullable(hoja.Cell(filaTotal, 41),
                        Diferencia(
                            SumaDecimalNullable(filas, x => x.RecuperacionVencidosActual),
                            SumaDecimalNullable(filas, x => x.MetaRecuperacionVencidosMes)));
                    AplicarTotal(hoja.Range(filaTotal, 1, filaTotal, 42));

                    hoja.Range(6, 6, filaTotal, 13).Style.NumberFormat.Format = Moneda;
                    hoja.Range(6, 14, filaTotal, 14).Style.NumberFormat.Format = Porcentaje;
                    hoja.Range(6, 16, filaTotal, 20).Style.NumberFormat.Format = Entero;
                    hoja.Range(6, 21, filaTotal, 21).Style.NumberFormat.Format = Porcentaje;
                    hoja.Range(6, 23, filaTotal, 26).Style.NumberFormat.Format = Moneda;
                    hoja.Range(6, 27, filaTotal, 27).Style.NumberFormat.Format = Porcentaje;
                    hoja.Range(6, 28, filaTotal, 30).Style.NumberFormat.Format = Entero;
                    hoja.Range(6, 31, filaTotal, 32).Style.NumberFormat.Format = Moneda;
                    hoja.Range(6, 34, filaTotal, 34).Style.NumberFormat.Format = Entero;
                    hoja.Range(6, 35, filaTotal, 37).Style.NumberFormat.Format = Moneda;
                    hoja.Range(6, 38, filaTotal, 38).Style.NumberFormat.Format = Entero;
                    hoja.Range(6, 39, filaTotal, 41).Style.NumberFormat.Format = Moneda;

                    hoja.Range(5, 1, filaTotal, 42).SetAutoFilter();
                    hoja.Range(5, 1, filaTotal, 42).Style.Border.BottomBorder =
                        XLBorderStyleValues.Hair;
                    hoja.Range(5, 1, filaTotal, 42).Style.Border.BottomBorderColor =
                        XLColor.FromHtml("#D9E1F2");

                    /* Bloque identificador compacto: permanece visible al desplazarse.
                       Se repiten los valores para conservar filtros y ordenamiento. */
                    hoja.Range(6, 1, filaTotal - 1, 2).Style.Fill.BackgroundColor =
                        XLColor.FromHtml("#F2F7FB");
                    hoja.Range(6, 3, filaTotal - 1, 3).Style.Fill.BackgroundColor =
                        XLColor.FromHtml("#D9EAF7");
                    hoja.Range(6, 3, filaTotal - 1, 3).Style.Font.Bold = true;
                    hoja.Range(6, 1, filaTotal - 1, 3).Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Left;
                    hoja.Range(4, 3, filaTotal, 3).Style.Border.RightBorder =
                        XLBorderStyleValues.Medium;
                    hoja.Range(4, 3, filaTotal, 3).Style.Border.RightBorderColor =
                        XLColor.FromHtml("#17365D");

                    hoja.SheetView.FreezeRows(5);
                    hoja.SheetView.FreezeColumns(3);

                    hoja.Columns(1, 42).Width = 14;
                    hoja.Column(1).Width = 10;
                    hoja.Column(2).Width = 12;
                    hoja.Column(3).Width = 15;
                    hoja.Column(4).Width = 22;
                    hoja.Column(5).Width = 12;
                    hoja.Column(15).Width = 17;
                    hoja.Column(22).Width = 17;
                    hoja.Column(33).Width = 18;
                    hoja.Column(42).Width = 18;
                    EscribirPie(hoja, filaTotal + 2, 42);
                    ConfigurarImpresion(hoja, "Detalle analistas · " + NombrePeriodo(filas[0].Periodo), fitOnePage: false);
                }

                private static void CrearDefiniciones(
                    XLWorkbook libro,
                    AvanceMetaGerencialDto meta)
                {
                    var hoja = libro.Worksheets.Add("Definiciones");
                    hoja.TabColor = XLColor.FromHtml("#" + GrisTexto);
                    PrepararHoja(hoja);
                    EscribirMarca(hoja, "A1:D1");
                    EscribirTitulo(hoja, "A2:D2", "DEFINICIONES DEL REPORTE");
                    EscribirEncabezados(hoja, 4,
                        new[] { "Concepto", "Definición", "Origen", "Observación" });

                    var filas = new object[][]
                    {
                        new object[] { "Saldo total al cierre", "Saldo total esperado o real al finalizar el mes.", "Plan de pagos menos pagos válidos", "Incluye cuota y cargo." },
                        new object[] { "Capital productivo neto", "Saldo total menos vencidos para las carteras productivas.", "Saldo total al cierre menos vencidos", "Permite reconciliar el criterio usado en el Excel manual del CEO." },
                        new object[] { "Clientes activos", "Personas con créditos activos al cierre.", "CREDITO.Credito", "Se cuentan personas distintas." },
                        new object[] { "Clientes nuevos", "Personas distintas registradas como clientes dentro del mes.", "MAESTRO.Cliente + CREDITO.Credito", "Se atribuyen al analista dueño del crédito; no son clientes activos." },
                        new object[] { "Monto clientes nuevos", "Monto desembolsado durante el mes a los clientes registrados en ese mismo mes.", "CREDITO.Credito + MAESTRO.Cliente", "Se atribuye al analista dueño del crédito." },
                        new object[] { "Monto cobrado", "Suma de cobranzas CUO válidas registradas dentro del mes.", "CREDITO.MovimientoCaja", "Estado=1, IndEntrada=1 e ImportePago>0." },
                        new object[] { "Desembolsos", "Monto desembolsado en créditos válidos durante el mes.", "CREDITO.Credito.FechaDesembolso", "Incluye créditos DES y PAG." },
                        new object[] { "N.º operaciones", "Cantidad de créditos desembolsados durante el mes.", "CREDITO.Credito.FechaDesembolso", "No equivale al número de clientes." },
                        new object[] { "Variación vs base", "Resultado actual menos el cierre del mes anterior.", "Cierre anterior y periodo consultado", "No depende de que gerencia haya configurado una meta." },
                        new object[] { "Brecha vs meta", "Resultado actual menos la meta registrada por gerencia.", "Meta mensual", "Queda vacía cuando la meta no está configurada." },
                        new object[] { "Reducción de vencidos", "Vencidos de la base menos vencidos actuales.", "Cierres comparables", "Un valor positivo representa una mejora." },
                        new object[] { "Vencidos", "Saldo residual de cuotas vencidas a la fecha de evaluación.", "Plan de pagos con aplicación FIFO", "No utiliza la penalidad de mora del 1% diario." },
                        new object[] { "Recuperación de vencidos", "Reducción atribuible a pagos sobre las cuotas vencidas existentes en la apertura.", "Detalle de apertura mensual", "Requiere una base comparable." },
                        new object[] { "Pago válido", "Movimiento CUO activo, de entrada y con importe positivo.", "CREDITO.MovimientoCaja", "Estado=1 e IndEntrada=1." },
                        new object[] { "Cartera especial", "Cartera destinada al seguimiento de mora.", "Configuración gerencial", "Capital y clientes no tienen meta." },
                        new object[] { "Fecha oficial", "Fecha y hora del módulo de cierre gerencial.", "CREDITO.ufn_FechaGerencial()", meta.FechaCalculo.ToString("dd/MM/yyyy HH:mm:ss") }
                    };

                    for (var i = 0; i < filas.Length; i++)
                        EscribirFila(hoja, 5 + i, filas[i]);

                    var ultimaFila = 4 + filas.Length;
                    hoja.Range(4, 1, ultimaFila, 4).Style.Border.BottomBorder =
                        XLBorderStyleValues.Hair;
                    hoja.Range(4, 1, ultimaFila, 4).Style.Border.BottomBorderColor =
                        XLColor.FromHtml("#D9E1F2");
                    hoja.Range(5, 1, ultimaFila, 4).Style.Alignment.WrapText = true;
                    hoja.Column("A").Width = 28;
                    hoja.Column("B").Width = 58;
                    hoja.Column("C").Width = 36;
                    hoja.Column("D").Width = 42;
                    EscribirPie(hoja, ultimaFila + 2, 4);
                    ConfigurarImpresion(hoja, "Definiciones", fitOnePage: true);
                }

                private static void CrearLeyenda(XLWorkbook libro)
                {
                    var hoja = libro.Worksheets.Add("Leyenda");
                    hoja.TabColor = XLColor.FromHtml("#" + VerdeTexto);
                    PrepararHoja(hoja);
                    EscribirMarca(hoja, "A1:C1");
                    EscribirTitulo(hoja, "A2:C2", "LEYENDA DE ESTADOS Y LECTURA");
                    EscribirEncabezados(hoja, 4, new[] { "Elemento", "Significado", "Uso" });

                    var filas = new object[][]
                    {
                        new object[] { "CUMPLIDA / CUMPLIDO / OK", "La meta o indicador se alcanzó.", "Capital, clientes, vencidos, recuperación" },
                        new object[] { "EN RIESGO / PARCIAL", "Avance intermedio; requiere seguimiento.", "Estados de cumplimiento" },
                        new object[] { "INCUMPLIDA / INCUMPLIDO", "La meta no se alcanzó.", "Estados de cumplimiento" },
                        new object[] { "SIN CONFIGURAR", "Aún no hay meta definitiva registrada.", "Columna de estado final" },
                        new object[] { "PRODUCTIVA", "Cartera ordinaria con meta de capital y clientes.", "Clasificación" },
                        new object[] { "ESPECIAL", "Cartera de mora; capital/clientes no aplican.", "Clasificación" },
                        new object[] { "Variación favorable", "Fondo verde: mejora respecto a la base o meta.", "Columnas de variación / brecha" },
                        new object[] { "Variación desfavorable", "Fondo rojo: deterioro respecto a la base o meta.", "Columnas de variación / brecha" },
                        new object[] { "Cierre oficial", "Consolidado tras cierre de bóveda (fin de mes o días 1–2).", "Banner del resumen" },
                        new object[] { "Avance no oficial", "Cifras preliminares sujetas a cambio.", "Banner del resumen" }
                    };

                    for (var i = 0; i < filas.Length; i++)
                        EscribirFila(hoja, 5 + i, filas[i]);

                    var ultima = 4 + filas.Length;
                    hoja.Range(4, 1, ultima, 3).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                    hoja.Range(4, 1, ultima, 3).Style.Border.BottomBorderColor = XLColor.FromHtml("#D9E1F2");
                    hoja.Range(5, 1, ultima, 3).Style.Alignment.WrapText = true;
                    hoja.Column("A").Width = 28;
                    hoja.Column("B").Width = 58;
                    hoja.Column("C").Width = 36;
                    EscribirPie(hoja, ultima + 2, 3);
                    ConfigurarImpresion(hoja, "Leyenda", fitOnePage: true);
                }

                private static void EscribirMarca(IXLWorksheet hoja, string rango)
                {
                    hoja.Range(rango).Merge();
                    hoja.Range(rango).Value = Empresa + "  ·  Informe confidencial de gestión";
                    hoja.Range(rango).Style.Font.FontSize = 9;
                    hoja.Range(rango).Style.Font.Bold = true;
                    hoja.Range(rango).Style.Font.FontColor = XLColor.FromHtml("#" + AzulOscuro);
                    hoja.Range(rango).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    hoja.Range(rango).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F7FB");
                }

                private static void EscribirPie(IXLWorksheet hoja, int fila, int columnas)
                {
                    var rango = hoja.Range(fila, 1, fila, columnas);
                    rango.Merge();
                    rango.Value =
                        "Generado " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-PE")) +
                        "  ·  " + Empresa +
                        "  ·  Uso interno  ·  No altera la lógica de cálculo del cierre gerencial";
                    rango.Style.Font.FontSize = 8;
                    rango.Style.Font.FontColor = XLColor.FromHtml("#" + GrisTexto);
                    rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }

                private static void ConfigurarImpresion(IXLWorksheet hoja, string titulo, bool fitOnePage)
                {
                    hoja.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                    hoja.PageSetup.FitToPages(1, fitOnePage ? 1 : 0);
                    hoja.PageSetup.Header.Center.AddText(titulo);
                    hoja.PageSetup.Footer.Left.AddText(Empresa);
                    hoja.PageSetup.Footer.Right.AddText("Página &P de &N");
                    hoja.PageSetup.Margins.Header = 0.3;
                    hoja.PageSetup.Margins.Footer = 0.3;
                }

                private static void AplicarFilaDetalle(IXLWorksheet hoja, int fila, bool especial)
                {
                    var rango = hoja.Range(fila, 1, fila, 42);
                    if (especial)
                        rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + EspecialSoft);
                    else if ((fila % 2) == 0)
                        rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + Zebra);

                    hoja.Range(fila, 1, fila, 3).Style.Fill.BackgroundColor =
                        XLColor.FromHtml(especial ? "#" + EspecialSoft : "#F2F7FB");
                    hoja.Cell(fila, 3).Style.Fill.BackgroundColor =
                        XLColor.FromHtml("#" + AzulClaro);
                    hoja.Cell(fila, 3).Style.Font.Bold = true;
                }

                private static void PrepararHoja(IXLWorksheet hoja)
                {
                    hoja.ShowGridLines = false;
                    hoja.Style.Font.FontName = "Arial";
                    hoja.Style.Font.FontSize = 10;
                    hoja.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    hoja.Row(1).Height = 8;
                }

                private static void EscribirTitulo(
                    IXLWorksheet hoja,
                    string rango,
                    string titulo)
                {
                    hoja.Range(rango).Merge();
                    hoja.Range(rango).Value = titulo;
                    hoja.Range(rango).Style.Font.Bold = true;
                    hoja.Range(rango).Style.Font.FontSize = 15;
                    hoja.Range(rango).Style.Font.FontColor =
                        XLColor.FromHtml("#" + AzulOscuro);
                    hoja.Range(rango).Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Left;
                    hoja.Range(rango).Style.Border.BottomBorder =
                        XLBorderStyleValues.Thin;
                    hoja.Range(rango).Style.Border.BottomBorderColor =
                        XLColor.FromHtml("#" + Azul);
                    hoja.Row(2).Height = 25;
                }

                private static void EscribirKpi(
                    IXLWorksheet hoja,
                    string rangoTitulo,
                    string rangoValor,
                    string titulo,
                    object valor,
                    string formato,
                    string color)
                {
                    hoja.Range(rangoTitulo).Merge();
                    hoja.Range(rangoValor).Merge();
                    hoja.Range(rangoTitulo).Value = titulo;
                    AsignarValor(
                        hoja.Range(rangoValor).FirstCell(),
                        valor);
                    hoja.Range(rangoTitulo).Style.Fill.BackgroundColor =
                        XLColor.FromHtml("#" + color);
                    hoja.Range(rangoTitulo).Style.Font.FontColor = XLColor.White;
                    hoja.Range(rangoTitulo).Style.Font.Bold = true;
                    hoja.Range(rangoTitulo).Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;
                    hoja.Range(rangoValor).Style.Fill.BackgroundColor =
                        XLColor.FromHtml("#F7F9FB");
                    hoja.Range(rangoValor).Style.Font.FontSize = 14;
                    hoja.Range(rangoValor).Style.Font.Bold = true;
                    hoja.Range(rangoValor).Style.Font.FontColor =
                        XLColor.FromHtml("#" + AzulOscuro);
                    if (!string.IsNullOrWhiteSpace(formato))
                        hoja.Range(rangoValor).Style.NumberFormat.Format = formato;
                    hoja.Range(rangoValor).Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;
                    hoja.Range(rangoTitulo).Style.Border.OutsideBorder =
                        XLBorderStyleValues.Thin;
                    hoja.Range(rangoTitulo).Style.Border.OutsideBorderColor =
                        XLColor.FromHtml("#" + color);
                    hoja.Range(rangoValor).Style.Border.OutsideBorder =
                        XLBorderStyleValues.Thin;
                    hoja.Range(rangoValor).Style.Border.OutsideBorderColor =
                        XLColor.FromHtml("#D0D7DE");
                    hoja.Row(6).Height = 22;
                    hoja.Row(7).Height = 22;
                }

                private static void EscribirEncabezados(
                    IXLWorksheet hoja,
                    int fila,
                    string[] encabezados)
                {
                    for (var i = 0; i < encabezados.Length; i++)
                        hoja.Cell(fila, i + 1).Value = encabezados[i];

                    AplicarEncabezado(
                        hoja.Range(fila, 1, fila, encabezados.Length),
                        Azul);
                    hoja.Row(fila).Height = 34;
                    hoja.Range(fila, 1, fila, encabezados.Length)
                        .Style.Alignment.WrapText = true;
                }

                private static void AplicarEncabezado(
                    IXLRange rango,
                    string color)
                {
                    rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + color);
                    rango.Style.Font.FontColor = XLColor.White;
                    rango.Style.Font.Bold = true;
                    rango.Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;
                    rango.Style.Alignment.Vertical =
                        XLAlignmentVerticalValues.Center;
                    rango.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    rango.Style.Border.RightBorderColor = XLColor.White;
                }

                private static void EscribirFila(
                    IXLWorksheet hoja,
                    int fila,
                    object[] valores)
                {
                    for (var i = 0; i < valores.Length; i++)
                    {
                        var valor = valores[i];
                        if (valor != null)
                            AsignarValor(hoja.Cell(fila, i + 1), valor);
                    }
                }

                private static void AsignarValor(IXLCell celda, object valor)
                {
                    if (valor == null)
                        return;

                    if (valor is string)
                    {
                        celda.Value = (string)valor;
                        return;
                    }

                    if (valor is DateTime)
                    {
                        celda.Value = (DateTime)valor;
                        return;
                    }

                    if (valor is bool)
                    {
                        celda.Value = (bool)valor;
                        return;
                    }

                    if (valor is byte || valor is sbyte ||
                        valor is short || valor is ushort ||
                        valor is int || valor is uint ||
                        valor is long || valor is ulong ||
                        valor is float || valor is double ||
                        valor is decimal)
                    {
                        celda.Value = Convert.ToDouble(
                            valor,
                            CultureInfo.InvariantCulture);
                        return;
                    }

                    celda.Value = Convert.ToString(
                        valor,
                        CultureInfo.InvariantCulture);
                }

                private static void AplicarTotal(IXLRange rango)
                {
                    rango.Style.Fill.BackgroundColor =
                        XLColor.FromHtml("#" + AzulClaro);
                    rango.Style.Font.Bold = true;
                    rango.Style.Border.TopBorder = XLBorderStyleValues.Double;
                    rango.Style.Border.TopBorderColor =
                        XLColor.FromHtml("#" + AzulOscuro);
                }

                private static void AplicarEstado(IXLCell celda, string estado)
                {
                    var texto = (estado ?? "SIN CONFIGURAR")
                        .Trim()
                        .ToUpperInvariant();

                    celda.Value = texto;
                    celda.Style.Font.Bold = true;
                    celda.Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    if (texto == "CUMPLIDA" || texto == "CUMPLIDO" || texto == "OK")
                    {
                        celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + Verde);
                        celda.Style.Font.FontColor = XLColor.FromHtml("#" + VerdeTexto);
                    }
                    else if (texto == "INCUMPLIDA" || texto == "INCUMPLIDO")
                    {
                        celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + Rojo);
                        celda.Style.Font.FontColor = XLColor.FromHtml("#" + RojoTexto);
                    }
                    else if (texto == "EN RIESGO" || texto == "PARCIAL")
                    {
                        celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + Amarillo);
                        celda.Style.Font.FontColor = XLColor.FromHtml("#7F6000");
                    }
                    else
                    {
                        celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + Gris);
                        celda.Style.Font.FontColor = XLColor.FromHtml("#" + GrisTexto);
                    }
                }

                private static void AplicarVariacion(
                    IXLCell celda,
                    decimal? valor,
                    bool positivoEsBueno)
                {
                    if (!valor.HasValue || valor.Value == 0m)
                        return;

                    var favorable = positivoEsBueno
                        ? valor.Value > 0m
                        : valor.Value < 0m;

                    celda.Style.Font.Bold = true;
                    celda.Style.Fill.BackgroundColor = XLColor.FromHtml(
                        "#" + (favorable ? Verde : Rojo));
                    celda.Style.Font.FontColor = XLColor.FromHtml(
                        "#" + (favorable ? VerdeTexto : RojoTexto));
                }

                private static bool EsTipo(
                    AvanceMetaGerencialDto fila,
                    string tipo)
                {
                    return string.Equals(
                        fila.TipoCartera,
                        tipo,
                        StringComparison.OrdinalIgnoreCase);
                }

                private static decimal? CapitalProductivoNeto(
                    AvanceMetaGerencialDto fila)
                {
                    if (!EsTipo(fila, "PRODUCTIVA"))
                        return null;

                    var resultado = fila.CapitalActual - fila.VencidosActual;
                    return resultado < 0m ? 0m : resultado;
                }

                private static decimal? CapitalProductivoBase(
                    AvanceMetaGerencialDto fila)
                {
                    if (!EsTipo(fila, "PRODUCTIVA") || !fila.CapitalBase.HasValue)
                        return null;

                    var vencidos = fila.VencidosBaseLegacy ?? 0m;
                    var resultado = fila.CapitalBase.Value - vencidos;
                    return resultado < 0m ? 0m : resultado;
                }

                private static decimal? Variacion(decimal? valorBase, decimal? valorActual)
                {
                    return valorBase.HasValue && valorActual.HasValue
                        ? (decimal?)(valorActual.Value - valorBase.Value)
                        : null;
                }

                private static decimal? Variacion(int? valorBase, int valorActual)
                {
                    return valorBase.HasValue
                        ? (decimal?)(valorActual - valorBase.Value)
                        : null;
                }

                private static decimal? Reduccion(decimal? valorBase, decimal valorActual)
                {
                    return valorBase.HasValue
                        ? (decimal?)(valorBase.Value - valorActual)
                        : null;
                }

                private static decimal? Reduccion(int? valorBase, int valorActual)
                {
                    return valorBase.HasValue
                        ? (decimal?)(valorBase.Value - valorActual)
                        : null;
                }

                private static decimal? Diferencia(decimal? actual, decimal? meta)
                {
                    return actual.HasValue && meta.HasValue
                        ? (decimal?)(actual.Value - meta.Value)
                        : null;
                }

                private static decimal? VariacionTotalDecimal(
                    IEnumerable<AvanceMetaGerencialDto> filas,
                    Func<AvanceMetaGerencialDto, decimal?> selectorBase,
                    Func<AvanceMetaGerencialDto, decimal?> selectorActual)
                {
                    var datos = filas.ToList();
                    var baseTotal = SumaDecimalNullable(datos, selectorBase);
                    if (!baseTotal.HasValue)
                        return null;

                    return SumaDecimal(datos, selectorActual) - baseTotal.Value;
                }

                private static int? VariacionTotalEntero(
                    IEnumerable<AvanceMetaGerencialDto> filas,
                    Func<AvanceMetaGerencialDto, int?> selectorBase,
                    Func<AvanceMetaGerencialDto, int> selectorActual)
                {
                    var datos = filas.ToList();
                    var baseTotal = SumaEnteroNullable(datos, selectorBase);
                    return baseTotal.HasValue
                        ? (int?)(datos.Sum(selectorActual) - baseTotal.Value)
                        : null;
                }

                private static decimal? ReduccionTotalDecimal(
                    IEnumerable<AvanceMetaGerencialDto> filas,
                    Func<AvanceMetaGerencialDto, decimal?> selectorBase,
                    Func<AvanceMetaGerencialDto, decimal> selectorActual)
                {
                    var datos = filas.ToList();
                    var baseTotal = SumaDecimalNullable(datos, selectorBase);
                    return baseTotal.HasValue
                        ? (decimal?)(baseTotal.Value - datos.Sum(selectorActual))
                        : null;
                }

                private static int? ReduccionTotalEntero(
                    IEnumerable<AvanceMetaGerencialDto> filas,
                    Func<AvanceMetaGerencialDto, int?> selectorBase,
                    Func<AvanceMetaGerencialDto, int> selectorActual)
                {
                    var datos = filas.ToList();
                    var baseTotal = SumaEnteroNullable(datos, selectorBase);
                    return baseTotal.HasValue
                        ? (int?)(baseTotal.Value - datos.Sum(selectorActual))
                        : null;
                }

                private static string NombrePeriodo(DateTime periodo)
                {
                    var cultura = CultureInfo.GetCultureInfo("es-PE");
                    return periodo.ToString("MMMM yyyy", cultura).ToUpper(cultura);
                }

                private static decimal? AFraccion(decimal? porcentaje)
                {
                    return porcentaje.HasValue
                        ? (decimal?)(porcentaje.Value / 100m)
                        : null;
                }

                private static decimal SumaDecimal(
                    IEnumerable<AvanceMetaGerencialDto> filas,
                    Func<AvanceMetaGerencialDto, decimal?> selector)
                {
                    return filas.Sum(x => selector(x) ?? 0m);
                }

                private static decimal? SumaDecimalNullable(
                    IEnumerable<AvanceMetaGerencialDto> filas,
                    Func<AvanceMetaGerencialDto, decimal?> selector)
                {
                    var valores = filas.Select(selector)
                        .Where(x => x.HasValue)
                        .Select(x => x.Value)
                        .ToList();
                    return valores.Count == 0 ? (decimal?)null : valores.Sum();
                }

                private static int? SumaEnteroNullable(
                    IEnumerable<AvanceMetaGerencialDto> filas,
                    Func<AvanceMetaGerencialDto, int?> selector)
                {
                    var valores = filas.Select(selector)
                        .Where(x => x.HasValue)
                        .Select(x => x.Value)
                        .ToList();
                    return valores.Count == 0 ? (int?)null : valores.Sum();
                }

                private static void AsignarNullable(IXLCell celda, decimal? valor)
                {
                    if (valor.HasValue)
                        celda.Value = valor.Value;
                }

                private static void AsignarNullable(IXLCell celda, int? valor)
                {
                    if (valor.HasValue)
                        celda.Value = valor.Value;
                }
    }
}
