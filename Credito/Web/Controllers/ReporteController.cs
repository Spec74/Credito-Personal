using Helper;
using ITB.VENDIX.BL;
using ITB.VENDIX.DA;
using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using VendixWeb.Models;
using Web.Controllers.Credito;
namespace VendixWeb.Controllers
{
    [Autenticado]
    public class ReporteController : Controller
    {

        public ActionResult Almacen()
        {
            ViewBag.cboOficina = new SelectList(OficinaBL.Listar(x => x.Estado), "OficinaId", "Denominacion");
            return View(SerieArticuloBL.ObtenerIndicadoresAlmacen());
        }
        public ActionResult ConstanciaAlmacen(int pMovimientoId)
        {
            var mov = MovimientoBL.ObtenerEntradaSalida(pMovimientoId);
            var det = MovimientoDetBL.Listar(x => x.MovimientoId == pMovimientoId);

            return View(new ReporteConstanciaAlmacen { Cabecera = mov, Detalle = det });
        }
        public ActionResult CobrosdelDia()
        {
            //var mov = MovimientoBL.ObtenerEntradaSalida(pMovimientoId);
            //var det = MovimientoDetBL.Listar(x => x.MovimientoId == pMovimientoId);

            return View();
        }
        public ActionResult CobranzaPagos()
        {
            var oficinaId = VendixGlobal<int>.Obtener("OficinaId");
            ViewBag.cboOficina = new SelectList(OficinaBL.Listar(x => x.Estado), "OficinaId", "Denominacion", oficinaId);
            ViewBag.cboGestor = new SelectList(UsuarioBL.Listar(x => x.Estado, includeProperties: "Persona"), "UsuarioId", "Persona.NombreCompleto");
            return View();
        }

        [HttpGet]
        public JsonResult ObtenerCobranzaPagos(int? pGestorid, int? pOficinaid)
        {
            var datos = CreditoBL.ReporteCobranzaGestor(pGestorid, pOficinaid);
            
            var resultado = datos.Select(x => new
            {
                x.Nro,
                x.Cliente,
                x.FormaPago,
                MontoCredito = x.MontoCredito.ToString("N2"),
                Interes = x.Interes.ToString("N2"),
                MontoTotal = x.MontoTotal.HasValue ? x.MontoTotal.Value.ToString("N2") : "0.00",
                FechaPrimerPago = x.FechaPrimerPago.ToString("dd/MM/yyyy"),
                FechaVencimiento = x.FechaVencimiento.ToString("dd/MM/yyyy"),
                Saldo = x.Saldo.HasValue ? x.Saldo.Value.ToString("N2") : "0.00",
                TotalPago = x.TotalPago.HasValue ? x.TotalPago.Value.ToString("N2") : "0.00",
                Pagos = x.Pagos,
                PagosLista = !string.IsNullOrEmpty(x.Pagos) ? x.Pagos.Split(',').Select(p => p.Trim()).ToList() : new List<string>(),
                PagosCount = (!string.IsNullOrEmpty(x.Pagos) ? x.Pagos.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).Count(p =>
                {
                    var pagoTrim = p;
                    var match = Regex.Match(pagoTrim, "\\(([^)]+)\\)");
                    if (match.Success) pagoTrim = pagoTrim.Replace(match.Value, "").Trim();
                    decimal v;
                    if (decimal.TryParse(pagoTrim, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v > 0;
                    return !(pagoTrim.StartsWith("0") || pagoTrim.StartsWith("0.00"));
                }) : 0),
                ImpagosCount = (!string.IsNullOrEmpty(x.Pagos) ? x.Pagos.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).Count(p =>
                {
                    var pagoTrim = p;
                    var match = Regex.Match(pagoTrim, "\\(([^)]+)\\)");
                    if (match.Success) pagoTrim = pagoTrim.Replace(match.Value, "").Trim();
                    decimal v;
                    if (decimal.TryParse(pagoTrim, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v == 0;
                    return (pagoTrim.StartsWith("0") || pagoTrim.StartsWith("0.00") || pagoTrim == "0");
                }) : 0)
                ,DiasAtraso = x.DiasAtrazoMora
            }).ToList();

            var resumen = new
            {
                TotalClientes = datos.Count,
                TotalCredito = datos.Sum(x => x.MontoCredito).ToString("N2"),
                TotalPagado = datos.Sum(x => x.TotalPago ?? 0).ToString("N2"),
                TotalSaldo = datos.Sum(x => x.Saldo ?? 0).ToString("N2")
            };

            return Json(new { success = true, data = resultado, resumen = resumen }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult ExportarCobranzaPagosExcel(int? pGestorid, int? pOficinaid)
        {
            var datos = CreditoBL.ReporteCobranzaGestor(pGestorid, pOficinaid);
            
            // Obtener nombre del gestor y oficina para el título
            var gestorNombre = "Todos";
            var oficinaNombre = "Todas";
            
            if (pGestorid.HasValue)
            {
                var gestor = UsuarioBL.Obtener(x => x.UsuarioId == pGestorid.Value, "Persona");
                if (gestor != null) gestorNombre = gestor.Persona.NombreCompleto;
            }
            if (pOficinaid.HasValue)
            {
                var oficina = OficinaBL.Obtener(x => x.OficinaId == pOficinaid.Value);
                if (oficina != null) oficinaNombre = oficina.Denominacion;
            }

            // Construir HTML para Excel
            var sb = new System.Text.StringBuilder();
            
            sb.Append("<html xmlns:x=\"urn:schemas-microsoft-com:office:excel\">");
            sb.Append("<head>");
            sb.Append("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">");
            sb.Append("<style>");
            sb.Append("table { border-collapse: collapse; width: 100%; }");
            sb.Append("th { background-color: #343a40; color: white; font-weight: bold; padding: 8px; border: 1px solid #dee2e6; text-align: center; }");
            sb.Append("td { padding: 6px; border: 1px solid #dee2e6; }");
            sb.Append(".text-right { text-align: right; }");
            sb.Append(".text-center { text-align: center; }");
            sb.Append(".header-row { background-color: #f8f9fa; font-weight: bold; }");
            sb.Append(".pagos-row { background-color: #e3f2fd; }");
            sb.Append(".pago-realizado { background-color: #28a745; color: white; padding: 2px 5px; }");
            sb.Append(".pago-cero { background-color: #dc3545; color: white; padding: 2px 5px; }");
            sb.Append(".totales { background-color: #343a40; color: white; font-weight: bold; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            
            
            // Título del reporte
            sb.Append("<table>");
            sb.Append("<tr><td colspan='10' style='font-size: 16px; font-weight: bold; text-align: center; padding: 10px;'>REPORTE DE COBRANZA POR GESTOR</td></tr>");
            sb.Append("<tr><td colspan='10' style='text-align: center;'>Oficina: " + oficinaNombre + " | Gestor: " + gestorNombre + " | Fecha: " + VendixGlobal.GetFecha().ToString("dd/MM/yyyy HH:mm") + "</td></tr>");
            sb.Append("<tr><td colspan='10'>&nbsp;</td></tr>");
            sb.Append("</table>");
            
            // Tabla principal
            sb.Append("<table>");
            sb.Append("<thead>");
            sb.Append("<tr>");
            sb.Append("<th>Nro</th>");
            sb.Append("<th>Cliente</th>");
            sb.Append("<th>Tipo</th>");
            sb.Append("<th>Crédito</th>");
            sb.Append("<th>Interés</th>");
            sb.Append("<th>Monto Total</th>");
            sb.Append("<th>1er Pago</th>");
            sb.Append("<th>Vencimiento</th>");
            sb.Append("<th>Total Pagado</th>");
            sb.Append("<th>Saldo</th>");
            sb.Append("</tr>");
            sb.Append("</thead>");
            sb.Append("<tbody>");

            foreach (var item in datos)
            {
                // Fila principal del cliente
                sb.Append("<tr>");
                sb.Append("<td class='text-center'>" + item.Nro + "</td>");
                sb.Append("<td style='font-weight: bold; color: #0066cc;'>" + item.Cliente + "</td>");
                sb.Append("<td class='text-center'>" + item.FormaPago + "</td>");
                sb.Append("<td class='text-right'>" + item.MontoCredito.ToString("N2") + "</td>");
                sb.Append("<td class='text-right'>" + item.Interes.ToString("N2") + "</td>");
                sb.Append("<td class='text-right'>" + (item.MontoTotal.HasValue ? item.MontoTotal.Value.ToString("N2") : "0.00") + "</td>");
                sb.Append("<td class='text-center'>" + item.FechaPrimerPago.ToString("d/MM/yyyy") + "</td>");
                sb.Append("<td class='text-center'>" + item.FechaVencimiento.ToString("d/MM/yyyy") + "</td>");
                sb.Append("<td class='text-right' style='font-weight: bold;'>" + (item.TotalPago.HasValue ? item.TotalPago.Value.ToString("N2") : "0.00") + "</td>");
                sb.Append("<td class='text-right' style='font-weight: bold; color: #dc3545;'>" + (item.Saldo.HasValue ? item.Saldo.Value.ToString("N2") : "0.00") + "</td>");
                sb.Append("</tr>");
                
                // Filas de historial de pagos debajo del cliente
                if (!string.IsNullOrEmpty(item.Pagos))
                {
                    var pagos = item.Pagos.Split(',');
                    var pagosValidos = new System.Collections.Generic.List<(string monto, string fecha, bool esCero)>();
                    
                    foreach (var pago in pagos)
                    {
                        var pagoTrim = pago.Trim();
                        if (!string.IsNullOrEmpty(pagoTrim))
                        {
                            var monto = pagoTrim;
                            var fecha = "";
                            var matchFecha = System.Text.RegularExpressions.Regex.Match(pagoTrim, @"\(([^)]+)\)");
                            if (matchFecha.Success)
                            {
                                fecha = matchFecha.Groups[1].Value;
                                monto = pagoTrim.Replace(matchFecha.Value, "").Trim();
                            }
                            var esCero = monto.StartsWith("0.00") || monto.StartsWith("0 ") || monto == "0";
                            pagosValidos.Add((monto, fecha, esCero));
                        }
                    }

                    // Agrupar pagos en filas de 8 columnas (excluyendo Nro y Cliente)
                    int pagosPerRow = 8;
                    for (int rowStart = 0; rowStart < pagosValidos.Count; rowStart += pagosPerRow)
                    {
                        // Fila de montos
                        sb.Append("<tr style='color: #dc3545;'>");
                        sb.Append("<td></td>"); // Nro vacío
                        sb.Append("<td></td>"); // Cliente vacío
                        
                        for (int col = 0; col < pagosPerRow; col++)
                        {
                            int idx = rowStart + col;
                            if (idx < pagosValidos.Count)
                            {
                                var p = pagosValidos[idx];
                                var bgColor = p.esCero ? "#dc3545" : "#17a2b8";
                                sb.Append("<td class='text-center' style='background-color: " + bgColor + "; color: white; font-weight: bold;'>" + p.monto + "</td>");
                            }
                            else
                            {
                                sb.Append("<td></td>");
                            }
                        }
                        sb.Append("</tr>");
                        
                        // Fila de fechas
                        sb.Append("<tr style='color: #dc3545; font-size: 10px;'>");
                        sb.Append("<td></td>"); // Nro vacío
                        sb.Append("<td></td>"); // Cliente vacío
                        
                        for (int col = 0; col < pagosPerRow; col++)
                        {
                            int idx = rowStart + col;
                            if (idx < pagosValidos.Count)
                            {
                                var p = pagosValidos[idx];
                                var bgColor = p.esCero ? "#dc3545" : "#17a2b8";
                                sb.Append("<td class='text-center' style='background-color: " + bgColor + "; color: white;'>" + p.fecha + "</td>");
                            }
                            else
                            {
                                sb.Append("<td></td>");
                            }
                        }
                        sb.Append("</tr>");
                    }
                    // Contar pagos y impagos
                    var pagosCount = pagosValidos.Count(p => !p.esCero);
                    var impagosCount = pagosValidos.Count(p => p.esCero);
                    // Calcular días de atraso (diferencia entre fecha actual y fecha de vencimiento)
                    var diasAtraso = Math.Max(0, (VendixGlobal.GetFecha().Date - item.FechaVencimiento.Date).Days);
                    // Agregar fila con resumen de conteo a la izquierda (como en UI)
                    sb.Append("<tr>");
                    sb.Append("<td colspan='2' style='vertical-align: middle; font-weight: bold; color: #333;'>");
                    sb.Append("<div>Pagos: " + pagosCount + "<br/>Impagos: " + impagosCount + "<br/><span style='color:#c82333;'>días atraso: " + diasAtraso + "</span></div>");
                    sb.Append("</td>");
                    // Rellenar el resto de columnas vacías
                    for (int i = 0; i < pagosPerRow; i++) sb.Append("<td></td>");
                    sb.Append("</tr>");
                }
            }

            // Fila de totales
            sb.Append("<tr class='totales'>");
            sb.Append("<td colspan='3' style='text-align: right;'>TOTALES (" + datos.Count + " clientes):</td>");
            sb.Append("<td class='text-right'>" + datos.Sum(x => x.MontoCredito).ToString("N2") + "</td>");
            sb.Append("<td class='text-right'>" + datos.Sum(x => x.Interes).ToString("N2") + "</td>");
            sb.Append("<td class='text-right'>" + datos.Sum(x => x.MontoTotal ?? 0).ToString("N2") + "</td>");
            sb.Append("<td colspan='2'></td>");
            sb.Append("<td class='text-right'>" + datos.Sum(x => x.TotalPago ?? 0).ToString("N2") + "</td>");
            sb.Append("<td class='text-right'>" + datos.Sum(x => x.Saldo ?? 0).ToString("N2") + "</td>");
            sb.Append("</tr>");
            
            sb.Append("</tbody>");
            sb.Append("</table>");
            sb.Append("</body>");
            sb.Append("</html>");

            // Generar archivo Excel
            var fileName = "CobranzaPagos_" + VendixGlobal.GetFecha().ToString("yyyyMMdd_HHmmss") + ".xls";
            var contentType = "application/vnd.ms-excel";
            var fileBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            
            Response.AddHeader("content-disposition", "attachment; filename=" + fileName);
            
            return File(fileBytes, contentType, fileName);
        }

        public ActionResult Credito()
        {
            var lstoficina = new SelectList(OficinaBL.Listar(x => x.Estado), "OficinaId", "Denominacion");
            ViewBag.cboOficina = lstoficina;
            ViewBag.cboOficina1 = lstoficina;
            ViewBag.cboOficina2 = lstoficina;
            ViewBag.cboOficina3 = lstoficina;
            ViewBag.cboOficina4 = lstoficina;
            ViewBag.cboOficina5 = lstoficina;
            ViewBag.cboOficina6 = lstoficina;
            var usuario = new SelectList(UsuarioBL.Listar(x => x.Estado, includeProperties: "Persona"), "UsuarioId", "Persona.NombreCompleto");

            ViewBag.cboUsuario = usuario;
            ViewBag.cboGestor = usuario;
            ViewBag.cboGestor2 = usuario;
            ViewBag.cboGestor3 = usuario;

            var usuarioId = VendixGlobal<int>.Obtener("UsuarioId");
            var oficinaId = VendixGlobal<int>.Obtener("OficinaId");

            var rolParcial = UsuarioRolBL.Contar(x => x.UsuarioId == usuarioId
                                                           && x.OficinaId == oficinaId
                                                           && x.Rol.Denominacion == "REPORTEPARCIAL", includeProperties: "Rol") > 0;
            var rolAprobador = UsuarioRolBL.Contar(x => x.UsuarioId == usuarioId
                                                            && x.OficinaId == oficinaId
                                                            && x.Rol.Denominacion == "APROBADOR 1", includeProperties: "Rol") > 0;
            var rolAdmin = UsuarioRolBL.Contar(x => x.UsuarioId == usuarioId
                                                            && x.OficinaId == oficinaId
                                                            && x.Rol.Denominacion == "ADMINISTRADOR", includeProperties: "Rol") > 0;

            if (rolParcial) ViewBag.rol = "PARCIAL";
            if (rolAprobador) ViewBag.rol = "APROBADOR";
            if (rolAdmin) ViewBag.rol = "ADMIN";

            return View();
        }
        public ActionResult Venta()
        {
            ViewBag.cboMarca = new SelectList(MarcaBL.Listar(x => x.Estado, x => x.OrderBy(y => y.Denominacion)), "MarcaId", "Denominacion");
            ViewBag.cboOficina = new SelectList(OficinaBL.Listar(x => x.Estado), "OficinaId", "Denominacion");
            return View();
        }
        #region "ReportViewer"
        public ActionResult ReporteCajasAsignadas()
        {
            var oCajasAsignadas = CajaBL.LstCajaDiarioOficina();
            var oficinaid = VendixGlobal.GetOficinaId();
            var resumeningreso = CajaDiarioBL.ObtenerResumenCuentaCajaDiarios(oficinaid);
            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("ResumenIngreso",resumeningreso)
                                 };
            var rd = new ReportDataSource("dsCajasAsignadas", oCajasAsignadas);

            return Reporte("PDF", "rptCajasAsignadas.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteMovimientoCaja(int pMovimientoCajaId)
        {
            var operacion = MovimientoCajaBL.Obtener(pMovimientoCajaId).Operacion;
            if (operacion == "CUO")
            {
                if (PlanPagoBL.Contar(x => x.MovimientoCajaId == pMovimientoCajaId && x.Estado == "PAG") > 0)
                {
                    var data = MovimientoCajaBL.RptMovCajaCredito(pMovimientoCajaId);
                    var parametros = new List<ReportParameter>
                                         {
                                             new ReportParameter("MovimientoCajaId", data.MovimientoCajaId.ToString()),
                                             new ReportParameter("PersonaId", data.PersonaId.ToString()),
                                             new ReportParameter("Cliente", data.Cliente),
                                             new ReportParameter("User", data.User),
                                             new ReportParameter("FechaReg", data.FechaReg.ToString()),
                                             new ReportParameter("Oficina", data.Oficina),
                                             new ReportParameter("Producto", data.Producto),
                                             new ReportParameter("SaldoAnterior", data.SaldoAnterior.ToString()),
                                             new ReportParameter("PagoDeuda", data.PagoDeuda.ToString()),
                                             new ReportParameter("Interes", data.Interes.ToString()),
                                             new ReportParameter("MoraCargo", data.MoraCargo.ToString()),
                                             new ReportParameter("Descuento", data.Descuento.ToString()),
                                             new ReportParameter("ImporteLibreAnt", data.ImporteLibreAnt.ToString()),
                                             new ReportParameter("ImporteLibre", data.ImporteLibre.ToString()),
                                             new ReportParameter("ImportePagado", data.ImportePagado.ToString()),
                                             new ReportParameter("SaldoCapital", data.SaldoCapital.ToString()),
                                             new ReportParameter("CuotasPagadas", data.CuotasPagadas),
                                             new ReportParameter("ProximaCuota", data.ProximaCuota),
                                             new ReportParameter("CuotasAtrazadas", data.CuotasAtrazadas.ToString()),
                                             new ReportParameter("MoraTotalPendiente", data.MoraTotalPendiente.ToString()),
                                             new ReportParameter("EstadoCredito", data.EstadoCredito),
                                             new ReportParameter("CreditoTotal", data.CreditoTotal.ToString())
                                         };
                    return Reporte("PDF", "rptMovCajaCuota.rdlc", null, "TicketCaja", parametros);
                }
                else
                {
                    var data = MovimientoCajaBL.RptMovCajaLibre(pMovimientoCajaId);
                    var parametros = new List<ReportParameter>
                                         {
                                             new ReportParameter("MovimientoCajaId", data.MovimientoCajaId.ToString()),
                                             new ReportParameter("PersonaId", data.PersonaId.ToString()),
                                             new ReportParameter("Cliente", data.Cliente),
                                             new ReportParameter("User", data.User),
                                             new ReportParameter("FechaReg", data.FechaReg.ToString()),
                                             new ReportParameter("Oficina", data.Oficina),
                                             new ReportParameter("Producto", data.Producto),
                                             new ReportParameter("SaldoAnterior", "0.00"),
                                             new ReportParameter("PagoDeuda", "0.00"),
                                             new ReportParameter("Interes", "0.00"),
                                             new ReportParameter("MoraCargo", "0.00"),
                                             new ReportParameter("Descuento", "0.00"),
                                             new ReportParameter("ImporteLibreAnt", "0.00"),
                                             new ReportParameter("ImporteLibre", data.ImportePago.ToString()),
                                             new ReportParameter("ImportePagado", data.ImportePago.ToString()),
                                             new ReportParameter("SaldoCapital", data.SaldoCapital.ToString()),
                                             new ReportParameter("CuotasPagadas", data.CuotasPagadas.ToString()),
                                             new ReportParameter("ProximaCuota", data.ProximaCuota.ToString()),
                                             new ReportParameter("CuotasAtrazadas", data.CuotasAtrazadas.ToString()),
                                             new ReportParameter("MoraTotalPendiente", data.MoraTotalPendiente.ToString()),
                                             new ReportParameter("EstadoCredito", data.EstadoCredito),
                                             new ReportParameter("CreditoTotal", data.CreditoTotal.ToString())
                                         };
                    return Reporte("PDF", "rptMovCajaCuota.rdlc", null, "TicketCaja", parametros);
                }
            }
            if (operacion == "INI" || operacion == "GAD" || operacion == "CDN")
            {
                var data = MovimientoCajaBL.RptMovCajaInicial(pMovimientoCajaId);
                var concepto = string.Empty;
                var nrocredito = data.CreditoId == null ? "" : " CREDITO " + data.CreditoId.ToString();
                switch (operacion)
                {
                    case "INI": concepto = "PAGO INICIAL"; break;
                    case "GAD": concepto = "GASTO ADM ADELANTADO"; break;
                    case "CDN": concepto = "PAGO POR CONDONACION \n --CREDITO CANCELADO--"; break;
                }


                var parametros = new List<ReportParameter>
                                     {
                                         new ReportParameter("MovimientoCajaId", data.MovimientoCajaId.ToString()),
                                         new ReportParameter("PersonaId", data.PersonaId.ToString()),
                                         new ReportParameter("Cliente", data.Cliente),
                                         new ReportParameter("User", data.User),
                                         new ReportParameter("FechaReg", data.FechaReg.ToString()),
                                         new ReportParameter("Oficina", data.Oficina),
                                         new ReportParameter("Producto", data.Producto + nrocredito ),
                                         new ReportParameter("ImportePago", data.ImportePago.ToString()),
                                         new ReportParameter("Articulo", data.Articulo),
                                         new ReportParameter("Concepto", concepto)
                                     };
                return Reporte("PDF", "rptMovCaja.rdlc", null, "TicketCaja", parametros);
            }
            if (operacion == "CON")
            {
                var data = MovimientoCajaBL.RptMovCajaContado(pMovimientoCajaId);
                var parametros = new List<ReportParameter>
                                     {
                                         new ReportParameter("MovimientoCajaId", data.MovimientoCajaId.ToString()),
                                         new ReportParameter("PersonaId", data.PersonaId.ToString()),
                                         new ReportParameter("Cliente", data.Cliente),
                                         new ReportParameter("User", data.User),
                                         new ReportParameter("FechaReg", data.FechaReg.ToString()),
                                         new ReportParameter("Oficina", data.Oficina),
                                         new ReportParameter("Producto", data.Producto),
                                         new ReportParameter("ImportePago", data.ImportePago.ToString()),
                                         new ReportParameter("Articulo", data.Articulo),
                                         new ReportParameter("Concepto", "PAGO CONTADO")
                                     };
                return Reporte("PDF", "rptMovCaja.rdlc", null, "TicketCaja", parametros);
            }

            var dato = MovimientoCajaBL.RptMovCajaOtros(pMovimientoCajaId);
            if (string.IsNullOrEmpty(dato.Articulo)) dato.Articulo = "*";
            var param = new List<ReportParameter>
                                     {
                                         new ReportParameter("MovimientoCajaId", dato.MovimientoCajaId.ToString()),
                                         new ReportParameter("PersonaId", "0"),
                                         new ReportParameter("Cliente", dato.Cliente),
                                         new ReportParameter("User", dato.User),
                                         new ReportParameter("FechaReg", dato.FechaReg.ToString()),
                                         new ReportParameter("Oficina", dato.Oficina),
                                         new ReportParameter("Producto", dato.Producto),
                                         new ReportParameter("ImportePago", dato.ImportePago.ToString()),
                                         new ReportParameter("Articulo", dato.Articulo),
                                         new ReportParameter("Concepto", dato.IndEntrada?"ENTRADA":"SALIDA")
                                     };
            return Reporte("PDF", "rptMovCaja.rdlc", null, "TicketCaja", param);

        }
        public ActionResult ReporteMovimientoCajaChica(int pMovimientoCajaChicaId)
        {
            //var operacion = MovimientoCajaChicaBL.Obtener(pMovimientoCajaChicaId).Operacion;
            //if (operacion == "GAS")
            //{

            //        var data = MovimientoCajaBL.RptMovCajaLibre(pMovimientoCajaId);
            //        var parametros = new List<ReportParameter>
            //                             {
            //                                 new ReportParameter("MovimientoCajaId", data.MovimientoCajaId.ToString()),
            //                                 new ReportParameter("PersonaId", data.PersonaId.ToString()),
            //                                 new ReportParameter("Cliente", data.Cliente),
            //                                 new ReportParameter("User", data.User),
            //                                 new ReportParameter("FechaReg", data.FechaReg.ToString()),
            //                                 new ReportParameter("Oficina", data.Oficina),
            //                                 new ReportParameter("Producto", data.Producto),
            //                                 new ReportParameter("ImportePago", data.ImportePago.ToString()),
            //                                 new ReportParameter("Articulo", data.Articulo),
            //                                 new ReportParameter("Concepto", "PAGO LIBRE")
            //                             };
            //        return Reporte("PDF", "rptMovCaja.rdlc", null, "TicketCaja", parametros);

            //}           


            var dato = MovimientoCajaChicaBL.RptMovCajaOtros(pMovimientoCajaChicaId);
            if (string.IsNullOrEmpty(dato.Articulo)) dato.Articulo = "*";
            var param = new List<ReportParameter>
                                     {
                                         new ReportParameter("MovimientoCajaId", dato.MovimientoCajaId.ToString()),
                                         new ReportParameter("PersonaId", "0"),
                                         new ReportParameter("Cliente", dato.Cliente),
                                         new ReportParameter("User", dato.User),
                                         new ReportParameter("FechaReg", dato.FechaReg.ToString()),
                                         new ReportParameter("Oficina", dato.Oficina),
                                         new ReportParameter("Producto", dato.Producto),
                                         new ReportParameter("ImportePago", dato.ImportePago.ToString()),
                                         new ReportParameter("Articulo", dato.Articulo),
                                         new ReportParameter("Concepto", dato.IndEntrada?"ENTRADA":"SALIDA")
                                     };
            return Reporte("PDF", "rptMovCaja.rdlc", null, "TicketCaja", param);

        }
        public ActionResult ReporteCodBarras(int pMovimientoId)
        {
            var data = SerieArticuloBL.ListarArticuloCodigoBarras(pMovimientoId);
            var rd = new ReportDataSource("dsCodigo", data);
            return Reporte("PDF", "rptCodigo.rdlc", rd, "CodigoBarras");
        }
        public ActionResult ReporteKardex(int pArticuloId, int pAlmacenId)
        {
            var kardexData = AlmacenBL.GenerarKardex(pArticuloId, pAlmacenId);
            var rd = new ReportDataSource("dsKardex", kardexData);
            return Reporte("PDF", "rptKardex.rdlc", rd, "A4Horizontal0.25");
        }

        public ActionResult ReporteSimuladorPlanPagos(int pProductoId, decimal pMonto, int pCuotas, decimal pInteres,
string pFecha, string pModalidad, decimal? pGastosAdm = null, string pGA = "CAP",
string pCliente = "", string pTipoDoc = "", string pNroDoc = "", string pDirCliente = "",
string pDirNegocio = "", string pPrenda = "", bool pIncluyeCentral = false)
        {
            if (string.IsNullOrEmpty(pModalidad)) pModalidad = "M";

            // =========================================================================
            // INTEGRACIÓN DE GASTOS: Cálculo automático si vienen en 0 de la pantalla
            // =========================================================================
            if (!pGastosAdm.HasValue || pGastosAdm == 0)
            {
                pGastosAdm = ITB.VENDIX.BL.GastosAdmBL.CalcularGastosAdm(pMonto, pIncluyeCentral);
            }
            // =========================================================================

            // CORRECCIÓN MATEMÁTICA: El desembolso NO se resta. Es igual al monto solicitado.
            decimal desemb = pMonto;

            decimal pga = 0;
            if (pGA == "CUO" || pGA == "CAP")
                pga = pGastosAdm ?? 0;

            DateTime fechaPrimerPago;
            if (!DateTime.TryParse(pFecha, out fechaPrimerPago))
            {
                fechaPrimerPago = DateTime.Now;
            }

            // Ejecución de la lógica del SP con el gasto administrativo procesado
            var oPlanPago = CreditoBL.SimuladorCredito(pMonto, pModalidad, pCuotas, pInteres, fechaPrimerPago, pga);
            var rd = new Microsoft.Reporting.WebForms.ReportDataSource("dsSimuladorPlanPago", oPlanPago);

            string modalidadTexto = "MENSUAL";
            switch (pModalidad)
            {
                case "D": modalidadTexto = "DIARIO"; break;
                case "S": modalidadTexto = "SEMANAL"; break;
                case "Q": modalidadTexto = "QUINCENAL"; break;
                case "M": modalidadTexto = "MENSUAL"; break;
                default: modalidadTexto = "MENSUAL"; break;
            }

            var producto = ProductoBL.Obtener(pProductoId);
            string nombreProducto = producto != null ? producto.Denominacion : "PRODUCTO";

            // --- CÁLCULOS INTEGRADOS PARA LAS TARJETAS Y CUADROS RESUMEN ---
            decimal totalInteres = oPlanPago.Sum(x => x.Interes ?? 0);
            decimal totalDevolver = pMonto + totalInteres;
            string fechaUltimoPago = oPlanPago.LastOrDefault()?.FechaPago?.ToString("dd/MM/yyyy") ?? "-";
            decimal cuotaFila = oPlanPago.FirstOrDefault()?.Cuota ?? 0;

            // Recuperación dinámica del asesor logueado en el sistema
            var usuarioId = VendixGlobal.GetUsuarioId();
            string nombreAsesorGlobal = UsuarioBL.Obtener(x => x.UsuarioId == usuarioId, includeProperties: "Persona").Persona.NombreCompleto;
            string pAsesor = !string.IsNullOrWhiteSpace(nombreAsesorGlobal) ? nombreAsesorGlobal : (Request["sim_p.Asesor"]);
            string pTelefono = Request["sim_p.Telefono"];

            var parametros = new List<Microsoft.Reporting.WebForms.ReportParameter>
    {
        new Microsoft.Reporting.WebForms.ReportParameter("Monto", "S/. " + pMonto.ToString("F2")),
        new Microsoft.Reporting.WebForms.ReportParameter("Cuotas", pCuotas.ToString()),
        new Microsoft.Reporting.WebForms.ReportParameter("Producto", nombreProducto),
        new Microsoft.Reporting.WebForms.ReportParameter("Fecha", string.IsNullOrEmpty(pFecha) ? fechaPrimerPago.ToString("yyyy-MM-dd") : pFecha),
        new Microsoft.Reporting.WebForms.ReportParameter("Modalidad", modalidadTexto),
        new Microsoft.Reporting.WebForms.ReportParameter("TEM", pInteres + " %"),
        
        // El desembolso muestra el valor real completo (S/. 5000.00)
        new Microsoft.Reporting.WebForms.ReportParameter("Desembolso", "S/. " + desemb.ToString("F2")),
        new Microsoft.Reporting.WebForms.ReportParameter("GastosAdm", "S/. " + Math.Round(pGastosAdm ?? 0, 2).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
        
        // --- ASIGNACIÓN DE DATOS REALEZ DEL CLIENTE REGISTRADO (No vacíos) ---
        new Microsoft.Reporting.WebForms.ReportParameter("Cliente", string.IsNullOrWhiteSpace(pCliente) ? "CLIENTE PROSPECTO" : pCliente),
        new Microsoft.Reporting.WebForms.ReportParameter("TipoDocumento", string.IsNullOrWhiteSpace(pTipoDoc) ? "DNI" : (pTipoDoc == "N" ? "DNI" : "RUC")),
        new Microsoft.Reporting.WebForms.ReportParameter("NroDocumento", string.IsNullOrWhiteSpace(pNroDoc) ? "-" : pNroDoc),
        new Microsoft.Reporting.WebForms.ReportParameter("DireccionCliente", string.IsNullOrWhiteSpace(pDirCliente) ? "No Especificado" : pDirCliente),
        new Microsoft.Reporting.WebForms.ReportParameter("DireccionNegocio", string.IsNullOrWhiteSpace(pDirNegocio) ? "No Especificado" : pDirNegocio),
        new Microsoft.Reporting.WebForms.ReportParameter("PrendaDescripcion", string.IsNullOrWhiteSpace(pPrenda) ? "Ninguna" : pPrenda),

        // --- MAPEO DE PARÁMETROS DE TARJETAS INFERIORES ---
        new Microsoft.Reporting.WebForms.ReportParameter("Asesor", pAsesor),
        new Microsoft.Reporting.WebForms.ReportParameter("TelefonoCliente", pTelefono),
        new Microsoft.Reporting.WebForms.ReportParameter("InteresesTotales", "S/. " + totalInteres.ToString("F2")),
        new Microsoft.Reporting.WebForms.ReportParameter("TotalDevolver", "S/. " + totalDevolver.ToString("F2")),
        new Microsoft.Reporting.WebForms.ReportParameter("CuotaDiaria", "S/. " + cuotaFila.ToString("F2")),
        new Microsoft.Reporting.WebForms.ReportParameter("FechaUltimoPago", fechaUltimoPago)
    };

            return Reporte("PDF", "rptSimuladorPlanPago.rdlc", rd, "A4Vertical0.25", parametros);
        }

        public ActionResult ReporteCliente(int pPersonaId)
        {
            var EstadoCivil = string.Empty;
            var TipoVivienda = string.Empty;
            var ActividadEconomica = string.Empty;
            var obj = ClienteBL.Obtener(x => x.PersonaId == pPersonaId, "Persona");

            if (obj.Persona.EstadoCivilId.HasValue)
            {
                EstadoCivil = ValorTablaBL.Obtener(x => x.TablaId == 11 && x.ItemId == obj.Persona.EstadoCivilId)
                    .Denominacion;
            }
            if (obj.Persona.TipoViviendaId.HasValue)
            {
                TipoVivienda = ValorTablaBL.Obtener(x => x.TablaId == 12 && x.ItemId == obj.Persona.TipoViviendaId)
                    .Denominacion;
            }
            if (obj.ActividadEconId.HasValue)
            {
                ActividadEconomica = OcupacionBL.Obtener(x => x.OcupacionId == obj.ActividadEconId)
                    .Denominacion;
            }

            var nrocreditos = CreditoBL.Contar(x => x.PersonaId == pPersonaId && x.Estado == "DES");
            var rptAval = CreditoBL.ReporteAval(pPersonaId);

            var conyugue = new Persona();
            if (obj.Persona.ConyuguePersonaId.HasValue)
                conyugue = PersonaBL.Obtener(obj.Persona.ConyuguePersonaId.Value);

            var rd = new ReportDataSource("dsReporteAval", rptAval);

            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Creditos", nrocreditos.ToString()),
                                     new ReportParameter("Cliente", obj.Persona.NombreCompleto),
                                     new ReportParameter("NumeroDocumento", obj.Persona.NumeroDocumento),
                                     new ReportParameter("FechaNacimiento", obj.Persona.FechaNacimiento.HasValue?obj.Persona.FechaNacimiento.Value.ToShortDateString():""),
                                     new ReportParameter("Sexo", obj.Persona.Sexo),
                                     new ReportParameter("Direccion", obj.Persona.Direccion),
                                     new ReportParameter("DireccionRef",obj.Persona.DireccionRef),
                                     new ReportParameter("Celular",obj.Persona.Celular1),
                                     new ReportParameter("Conyugue", conyugue.NombreCompleto),
                                     new ReportParameter("ConyugueDNI", conyugue.NumeroDocumento),
                                     new ReportParameter("ConyugueCelular", conyugue.Celular1),
                                     new ReportParameter("TipoVivienda",TipoVivienda),
                                     new ReportParameter("EstadoCivil", EstadoCivil),
                                     new ReportParameter("Distrito", ""),
                                     new ReportParameter("ActividadEconomica", ActividadEconomica),
                                     new ReportParameter("Nota", obj.Nota),
                                     new ReportParameter("DireccionNegocio", obj.DireccionNegocio),
                                     new ReportParameter("DireccionNegocioref", obj.DireccionNegocioRef)
                                 };

            return Reporte("PDF", "rptCliente.rdlc", rd, "A4Vertical0.25", parametros);
        }
        public ActionResult ReportePlanPagos(int pCreditoId)
        {
            var credito = CreditoBL.Obtener(x => x.CreditoId == pCreditoId, "Persona");
            var oPlanPago = CreditoBL.ReportePlanPago(pCreditoId);
            var rd = new ReportDataSource("dsPlanPago", oPlanPago);

            string pModalidad = string.Empty;
            switch (credito.FormaPago)
            {
                case "D": pModalidad = "DIARIO"; break;
                case "S": pModalidad = "SEMANAL"; break;
                case "Q": pModalidad = "QUINCENAL"; break;
                case "M": pModalidad = "MENSUAL"; break;
            }

            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Monto", credito.MontoCredito.ToString()),
                                     new ReportParameter("Cuotas", credito.NumeroCuotas.ToString()),
                                     new ReportParameter("Producto", ProductoBL.Obtener(credito.ProductoId).Denominacion),
                                     new ReportParameter("Fecha", credito.FechaPrimerPago.ToShortDateString()),
                                     new ReportParameter("Modalidad", pModalidad),
                                     new ReportParameter("Cliente", credito.Persona.NombreCompleto),
                                     new ReportParameter("TEM", credito.Interes.ToString() + "%"),
                                     new ReportParameter("Desembolso", credito.MontoDesembolso.ToString()),
                                     new ReportParameter("GastosAdm", credito.MontoGastosAdm.ToString())
                                 };

            return Reporte("PDF", "rptPlanPago.rdlc", rd, "A4Vertical0.25", parametros);
        }
        public ActionResult ReporteMovimientoBoveda(int? pBovedaId, string pTipo = "PDF")
        {
            Boveda oBoveda;

            if (pBovedaId.HasValue)
                oBoveda = BovedaBL.Obtener(pBovedaId.Value);
            else
            {
                var idUsuario = VendixGlobal.GetUsuarioId();
                var idOficina = VendixGlobal.GetOficinaId();
                var encargado = UsuarioRolBL.Contar(x => x.UsuarioId == idUsuario
                                                        && x.OficinaId == idOficina
                                                        && x.Rol.Denominacion == "ENCARGADO", includeProperties: "Rol");
                if (encargado > 0)
                    oBoveda = BovedaBL.Obtener(x => x.OficinaId == idOficina && x.IndCierre == false && x.IndTemporal == true);
                else
                    oBoveda = BovedaBL.Obtener(x => x.OficinaId == idOficina && x.IndCierre == false && x.IndTemporal == false);
            }

            // --- NUEVO: CÁLCULO DINÁMICO DE SALDOS POR CADA BANCO ---
            decimal saldoEfectivo = 0, saldoYape = 0, saldoInterbank = 0, saldoBcp = 0, saldoBn = 0, saldoYapeHuanta = 0, saldoInterbankHuanta = 0, saldoBcpHuanta = 0;

            using (var db = new VENDIXEntities())
            {
                // 1. Traemos los saldos iniciales de la sesión de bóveda actual
                var cuentas = db.BovedaCuenta.Where(x => x.BovedaId == oBoveda.BovedaId).ToList();
                // 2. Traemos todos los movimientos activos registrados en esta bóveda
                var movimientos = db.BovedaMov.Where(x => x.BovedaId == oBoveda.BovedaId && x.Estado == true).ToList();

                // 3. Aplicamos la regla contable: Saldo Inicial + Entradas - Salidas
                saldoEfectivo = (cuentas.FirstOrDefault(x => x.TipoPagoId == 1)?.SaldoInicial ?? 0) + movimientos.Where(x => x.TipoPagoId == 1).Sum(x => x.IndEntrada ? x.Importe : -x.Importe);
                saldoYape = (cuentas.FirstOrDefault(x => x.TipoPagoId == 2)?.SaldoInicial ?? 0) + movimientos.Where(x => x.TipoPagoId == 2).Sum(x => x.IndEntrada ? x.Importe : -x.Importe);
                saldoInterbank = (cuentas.FirstOrDefault(x => x.TipoPagoId == 3)?.SaldoInicial ?? 0) + movimientos.Where(x => x.TipoPagoId == 3).Sum(x => x.IndEntrada ? x.Importe : -x.Importe);
                saldoBcp = (cuentas.FirstOrDefault(x => x.TipoPagoId == 4)?.SaldoInicial ?? 0) + movimientos.Where(x => x.TipoPagoId == 4).Sum(x => x.IndEntrada ? x.Importe : -x.Importe); // <-- CORREGIDO AQUÍ
                saldoBn = (cuentas.FirstOrDefault(x => x.TipoPagoId == 5)?.SaldoInicial ?? 0) + movimientos.Where(x => x.TipoPagoId == 5).Sum(x => x.IndEntrada ? x.Importe : -x.Importe);
                // NUEVOS: Cálculos matemáticos vivos para la Sucursal Huanta
                saldoYapeHuanta = (cuentas.FirstOrDefault(x => x.TipoPagoId == 6)?.SaldoInicial ?? 0) + movimientos.Where(x => x.TipoPagoId == 6).Sum(x => x.IndEntrada ? x.Importe : -x.Importe);
                saldoInterbankHuanta = (cuentas.FirstOrDefault(x => x.TipoPagoId == 7)?.SaldoInicial ?? 0) + movimientos.Where(x => x.TipoPagoId == 7).Sum(x => x.IndEntrada ? x.Importe : -x.Importe);
                saldoBcpHuanta = (cuentas.FirstOrDefault(x => x.TipoPagoId == 8)?.SaldoInicial ?? 0) + movimientos.Where(x => x.TipoPagoId == 8).Sum(x => x.IndEntrada ? x.Importe : -x.Importe);
            }

            var oRpt = BovedaBL.ReporteMovimientoBoveda(oBoveda.BovedaId);
            using (var db = new VENDIXEntities())
            {
                var movimientosTRA = db.BovedaMov
                    .Where(x => x.BovedaId == oBoveda.BovedaId && x.CodOperacion == "TRA")
                    .ToList();

                foreach (var rptItem in oRpt.Where(x => x.CodOperacion == "TRA"))
                {
                    // CORRECCIÓN 1: Cambiamos 'Id' por 'MovimientoBovedaId'
                    var mov = movimientosTRA.FirstOrDefault(x => x.MovimientoBovedaId == rptItem.MovimientoBovedaId);

                    if (mov != null && mov.UsuarioRegId != null)
                    {
                        var usuario = db.Usuario.FirstOrDefault(u => u.UsuarioId == mov.UsuarioRegId);
                        if (usuario != null)
                        {
                            // CORRECCIÓN 2: Usamos NombreUsuario como salvavidas inmediato para que compile ya mismo
                            rptItem.Agente = "Hacia: " + usuario.Persona.NombreCompleto;
                        }
                    }
                }
            }
            // =======================================================================

            var rd = new ReportDataSource("dsMovimientoBoveda", oRpt);

            string fechaInicioStr = oBoveda.FechaIniOperacion.ToString("dd/MM/yyyy HH:mm:ss");
            string fechaFinStr = oBoveda.FechaFinOperacion.HasValue
                ? oBoveda.FechaFinOperacion.Value.ToString("dd/MM/yyyy HH:mm:ss")
                : DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            // =======================================================================
            // 🛠️ RECALCULO DINÁMICO DE TOTALES DE CABECERA (TRA Y TRF INCLUIDOS)
            // =======================================================================
            // Sumamos dinámicamente las columnas de la lista que va directo al reporte
            decimal totalEntradas = oRpt.Sum(x => (decimal?)(x.Entrada) ?? 0);
            decimal totalSalidas = oRpt.Sum(x => (decimal?)(x.Salida) ?? 0);

            // El saldo final real es: Saldo Inicial + Entradas Totales - Salidas Totales
            decimal saldoFinalCalculado = oBoveda.SaldoInicial + totalEntradas - totalSalidas;
            // =======================================================================

            var parametros = new List<ReportParameter>
             {
                 new ReportParameter("SaldoInicial", oBoveda.SaldoInicial.ToString()),
                 new ReportParameter("Entradas", totalEntradas.ToString()),
                 new ReportParameter("Salidas", totalSalidas.ToString()),
                 new ReportParameter("SaldoFinal", saldoFinalCalculado.ToString()),
                 new ReportParameter("FechaInicio", fechaInicioStr),
                 new ReportParameter("FechaFin", fechaFinStr),
                 new ReportParameter("Estado", oBoveda.IndCierre ? "CERRADO" : "ABIERTO"),
                             
                 // INYECTAMOS LOS 5 NUEVOS PARÁMETROS BANCARIOS ORIGINALES
                 new ReportParameter("SaldoEfectivo", saldoEfectivo.ToString("N2")),
                 new ReportParameter("SaldoYape", saldoYape.ToString("N2")),
                 new ReportParameter("SaldoInterbank", saldoInterbank.ToString("N2")),
                 new ReportParameter("SaldoBcp", saldoBcp.ToString("N2")),
                 new ReportParameter("SaldoBn", saldoBn.ToString("N2")),

                 // NUEVOS: Enviamos las variables dinámicas de Huanta listos para tu archivo .rdlc
                 new ReportParameter("SaldoYapeHuanta", saldoYapeHuanta.ToString("N2")),
                 new ReportParameter("SaldoInterbankHuanta", saldoInterbankHuanta.ToString("N2")),
                 new ReportParameter("SaldoBcpHuanta", saldoBcpHuanta.ToString("N2"))
             };

            return Reporte(pTipo, "rptMovimientoBoveda.rdlc", rd, "A4Vertical0.25", parametros);
        }
        public ActionResult ReporteMovimientoBovedaMov(int pMovimientoBovedaId, string pTipo = "PDF")
        {
            var oRpt = BovedaMovBL.Obtener(pMovimientoBovedaId);

            // ⚠️ Si este método usa el mismo reporte "rptMovimientoBoveda.rdlc", 
            // entonces estos parámetros NO EXISTEN y causarán error.
            // Debes ajustar según corresponda.

            var parametros = new List<ReportParameter>
    {
        // Si usa el reporte modificado, solo envía:
        new ReportParameter("SaldoInicial", "0"),      // o el valor real si aplica
        new ReportParameter("Entradas", "0"),
        new ReportParameter("Salidas", "0"),
        new ReportParameter("SaldoFinal", "0"),
        new ReportParameter("Estado", oRpt.Estado ? "Activo" : "Anulado")
        
        // ❌ No enviar: MovimientoBovedaId, Importe, Entrada, Fecha, CajadiarioId
    };

            return Reporte(pTipo, "rptMovimientoBoveda.rdlc", null, "A4Vertical0.25", parametros);
        }
        public ActionResult ReporteEstadoCredito(int pCreditoId)
        {
            string pModalidad = string.Empty;
            var credito = CreditoBL.Obtener(x => x.CreditoId == pCreditoId, includeProperties: "Persona,Producto");
            var data = CreditoBL.ListarEstadoPlanPago(pCreditoId);
            var rd = new ReportDataSource("dsEstadoCredito", data);
            var total = credito.MontoCredito + data.Sum(x => x.Interes);
            switch (credito.FormaPago)
            {
                case "D": pModalidad = "DIARIO"; break;
                case "S": pModalidad = "SEMANAL"; break;
                case "Q": pModalidad = "QUINCENAL"; break;
                case "M": pModalidad = "MENSUAL"; break;
            }
            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Producto", credito.Producto.Denominacion),
                                     new ReportParameter("FechaInicio", credito.FechaPrimerPago.ToShortDateString()),
                                     new ReportParameter("FechaFin", credito.FechaVencimiento.ToShortDateString()),
                                     new ReportParameter("MontoCredito", credito.MontoCredito.ToString()),
                                     new ReportParameter("Modalidad", pModalidad),
                                     new ReportParameter("Cuotas", credito.NumeroCuotas.ToString()),
                                     new ReportParameter("Interes", credito.Interes.ToString()),
                                     new ReportParameter("Estado", credito.Estado),
                                     new ReportParameter("Codigo", credito.Persona.Codigo),
                                     new ReportParameter("Cliente", credito.Persona.NumeroDocumento + " " + credito.Persona.NombreCompleto),
                                     new ReportParameter("Analista", UsuarioBL.ObtenerNombre(credito.UsuarioRegId)),
                                     new ReportParameter("GastoAdm", credito.MontoGastosAdm.ToString()),
                                     new ReportParameter("Total", total.ToString())
                                 };
            return Reporte("PDF", "rptEstadoCredito.rdlc", rd, "A4Vertical0.25", parametros);
        }
        public ActionResult ReporteCredito(int? pOficinaId, int? pGestorid, string pEstadoCredito, string pFechaIni, string pFechaFin)
        {
            var data = ReporteBL.ListarReporteCredito(pOficinaId, pGestorid, pEstadoCredito, DateTime.Parse(pFechaIni), DateTime.Parse(pFechaFin));
            var rd = new ReportDataSource("dsCredito", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;
            var ogestor = "TODOS";
            if (pGestorid != null)
                ogestor = UsuarioBL.Obtener(x => x.UsuarioId == pGestorid.Value, includeProperties: "Persona").Persona.NombreCompleto;

            switch (pEstadoCredito)
            {
                case "CRE": pEstadoCredito = "SOLICITUDES DE CREDITO"; break;
                case "PEN": pEstadoCredito = "PENDIENTES"; break;
                case "PAG": pEstadoCredito = "PAGADOS"; break;
                case "DES": pEstadoCredito = "DESEMBOLSADOS"; break;
                case "ANU": pEstadoCredito = "ANULADOS"; break;
                case "REP": pEstadoCredito = "REPROGRAMADOS"; break;
            }

            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina", oficina),
                                     new ReportParameter("Gestor", ogestor),
                                     new ReportParameter("Estado", pEstadoCredito),
                                     new ReportParameter("FechaIni", DateTime.Parse(pFechaIni).ToShortDateString()),
                                     new ReportParameter("FechaFin", DateTime.Parse(pFechaFin).ToShortDateString())
                                 };
            return Reporte("PDF", "rptCredito.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteCreditoRentabilidad(string pFechaIni, string pFechaFin, string pEstadoCredito, bool indTodos, int? pOficinaId, string pTipo = "PDF")
        {
            if (indTodos)
            {
                pFechaIni = "01/01/2018";
                pFechaFin = VendixGlobal.GetFecha().ToShortDateString();
            }
            var data = CreditoBL.ReporteCreditoRentabilidad(pOficinaId, DateTime.Parse(pFechaIni), DateTime.Parse(pFechaFin), pEstadoCredito);
            var rd = new ReportDataSource("dsCreditoRentabilidad", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;

            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina", oficina),
                                     new ReportParameter("FechaIni", indTodos?"----": DateTime.Parse(pFechaIni).ToShortDateString()),
                                     new ReportParameter("FechaFin", DateTime.Parse(pFechaFin).ToShortDateString())
                                 };
            return Reporte(pTipo, "rptCreditoRentabilidad.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteComprobantesCajaChica(string pFechaIni, string pFechaFin, string pTipo = "PDF")
        {
            var data = MovimientoRendidoCajaChicaBL.ReporteComprobantesCajaChica(DateTime.Parse(pFechaIni), DateTime.Parse(pFechaFin));
            var rd = new ReportDataSource("dsComprobantesCajaChica", data);


            var parametros = new List<ReportParameter>
                                 {
                                    new ReportParameter("FechaReporte", VendixGlobal.GetFecha().ToString()),
                                     new ReportParameter("FechaInicio", DateTime.Parse(pFechaIni).ToShortDateString()),
                                     new ReportParameter("FechaFin", DateTime.Parse(pFechaFin).ToShortDateString())
                                 };
            return Reporte(pTipo, "rptComprobantesCajaChica.rdlc", rd, "A4Horizontal0.25", parametros);
        }

        public ActionResult ReporteComprobantesCajaAnulados(string pFechaIni, string pFechaFin, string pTipo = "PDF")
        {
            var data = MovimientoCajaBL.ReporteComprobantesCajaAnulados(DateTime.Parse(pFechaIni), DateTime.Parse(pFechaFin));

            var rd = new ReportDataSource("dsComprobantesCajaAnulados", data);

            var parametros = new List<ReportParameter>
                                 {
                                    new ReportParameter("FechaReporte", VendixGlobal.GetFecha().ToString()),
                                     new ReportParameter("FechaInicio", DateTime.Parse(pFechaIni).ToShortDateString()),
                                     new ReportParameter("FechaFin", DateTime.Parse(pFechaFin).ToShortDateString())
                                 };
            return Reporte(pTipo, "rptComprobantesCajaAnulados.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteCreditoAprobado(string pFecha, int? pUsuarioid, int? pOficinaid, string pTipo = "PDF")
        {

            var data = CreditoBL.ReporteCreditoAprobacion(DateTime.Parse(pFecha), pUsuarioid, pOficinaid);
            var rd = new ReportDataSource("dsCreditoAprobacion", data);

            var oficina = "TODOS";
            if (pOficinaid != null)
                oficina = OficinaBL.Obtener(pOficinaid.Value).Denominacion;
            var usuario = "TODOS";
            if (pUsuarioid != null)
                usuario = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioid.Value, includeProperties: "Persona").Persona.NombreCompleto;

            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina", oficina),
                                     new ReportParameter("Gestor", usuario),
                                     new ReportParameter("Fecha", DateTime.Parse(pFecha).ToShortDateString())
                                 };
            return Reporte(pTipo, "rptCreditoAprobacion.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteCreditoMorosidad(int? pOficinaId, string pFechaHasta, int pDiasAtrazoIni, int pDiasAtrazoFin)
        {
            var data = CreditoBL.ReporteCreditoMorosidad(pOficinaId, DateTime.Parse(pFechaHasta), pDiasAtrazoIni, pDiasAtrazoFin);
            var rd = new ReportDataSource("dsCreditoMorosidad", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;

            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina", oficina),
                                     new ReportParameter("HastaFecha",DateTime.Parse(pFechaHasta).ToShortDateString()),
                                     new ReportParameter("DiasAtrazoIni", pDiasAtrazoIni.ToString()),
                                     new ReportParameter("DiasAtrazoFin", pDiasAtrazoFin.ToString())
                                 };
            return Reporte("PDF", "rptCreditoMorosidad.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteClientesNuevosMes(int? pOficinaId, int? pUsuarioId,
            string pFechaIni = "", string pFechaFin = "", string pTipo = "PDF")
        {

            DateTime? fechaini = null, fechafin = null;
            if (string.IsNullOrEmpty(pFechaIni))
            {
                var fecha = VendixGlobal.GetFecha();
                fechaini = fecha.AddDays(-fecha.Day + 1);
                fechafin = fechaini.Value.AddMonths(1).AddDays(-1);
            }
            else
            {
                fechaini = DateTime.Parse(pFechaIni);
                fechafin = DateTime.Parse(pFechaFin);
            }

            var data = CreditoBL.ReporteClientesNuevosMes(pOficinaId, pUsuarioId,
                fechaini, fechafin);
            var rd = new ReportDataSource("dsCreditoObservado", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;
            var agente = "TODOS";
            if (pUsuarioId != null)
                agente = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioId.Value, includeProperties: "Persona").Persona.NombreCompleto;


            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina", oficina),
                                     new ReportParameter("Agente", agente),
                                     new ReportParameter("Titulo", "CLIENTES NUEVOS DEL "
                                                                   + fechaini.Value.ToShortDateString() + " AL " +  fechafin.Value.ToShortDateString() )
                                 };
            return Reporte(pTipo, "rptCreditoObservado.rdlc", rd, "A4Horizontal0.25", parametros);
        }

        public ActionResult ReporteCajaDiario(int? pOficinaId, int? pUsuarioId, string pFechaIni = "", string pFechaFin = "", string pTipo = "PDF")
        {

            DateTime? fechaini = null, fechafin = null;
            if (string.IsNullOrEmpty(pFechaIni))
            {
                var fecha = VendixGlobal.GetFecha();
                fechaini = fecha.AddDays(-fecha.Day + 1);
                fechafin = fechaini.Value.AddMonths(1).AddDays(-1);
            }
            else
            {
                fechaini = DateTime.Parse(pFechaIni);
                fechafin = DateTime.Parse(pFechaFin);
            }

            var data = CreditoBL.ReporteCajaDiario(pOficinaId, pUsuarioId, fechaini, fechafin);
            var rd = new ReportDataSource("dsReporteCajaDiario", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;
            var agente = "TODOS";
            if (pUsuarioId != null)
                agente = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioId.Value, includeProperties: "Persona").Persona.NombreCompleto;


            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina", oficina),
                                     new ReportParameter("Agente", agente),
                                     new ReportParameter("FechaInicio", fechaini.Value.ToShortDateString() ),
                                     new ReportParameter("FechaFin", fechafin.Value.ToShortDateString() )
                                 };
            return Reporte(pTipo, "rptCajaDiario.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteClienteTopeCredito(int? pOficinaId, int? pUsuarioId, string pTipo = "PDF")
        {
            var data = CreditoBL.ReporteClientesTopeCredito(pOficinaId, pUsuarioId);
            var rd = new ReportDataSource("dsClienteTopeCredito", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;
            var agente = "TODOS";
            if (pUsuarioId != null)
                agente = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioId.Value, includeProperties: "Persona").Persona.NombreCompleto;


            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Fecha", " AL " + VendixGlobal.GetFecha().ToShortDateString()),
                                     new ReportParameter("Oficina", oficina),
                                     new ReportParameter("Agente",agente  )
                                 };
            return Reporte(pTipo, "rptClienteTopeCredito.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteClienteBloqueado(int? pOficinaId, int? pUsuarioId, string pTipo = "PDF")
        {
            var data = CreditoBL.ReporteClientesBloqueados(pOficinaId, pUsuarioId);
            var rd = new ReportDataSource("dsClienteBloqueado", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;
            var agente = "TODOS";
            if (pUsuarioId != null)
                agente = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioId.Value, includeProperties: "Persona").Persona.NombreCompleto;


            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Fecha", " AL " + VendixGlobal.GetFecha().ToShortDateString()),
                                     new ReportParameter("Oficina", oficina),
                                     new ReportParameter("Agente",agente  )
                                 };
            return Reporte(pTipo, "rptClienteBloqueado.rdlc", rd, "A4Horizontal0.25", parametros);
        }

        public ActionResult ReporteClientesInactivos(int? pOficinaId, int? pUsuarioId, string pTipo = "PDF")
        {
            // 1. Invoca al SP optimizado
            var data = CreditoBL.ReporteClientesInactivos(pOficinaId, pUsuarioId, null, null);
            var rd = new ReportDataSource("dsClienteInactivo", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;

            var agente = "TODOS";
            if (pUsuarioId != null)
                agente = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioId.Value, includeProperties: "Persona").Persona.NombreCompleto;

            // Esto cuenta el total de FILAS (Clientes) de la lista
            var totalClientesStr = (data != null) ? data.Count.ToString() : "0";

            // 2. Definición de parámetros globales
            var parametros = new List<ReportParameter>
             {
                 new ReportParameter("Fecha", " AL " + VendixGlobal.GetFecha().ToShortDateString()),
                 new ReportParameter("Oficina", oficina),
                 new ReportParameter("Agente", agente),
                 new ReportParameter("TotalClientesInactivos", totalClientesStr) // ¡Correcto!
             };

            return Reporte(pTipo, "rptClienteInactivo.rdlc", rd, "A4Horizontal0.25", parametros);
        }

        public ActionResult ReporteClientesInactivosPagados(int? pOficinaId, int? pUsuarioId, string pFechaIni, string pFechaFin, string pTipo = "PDF")
        {
            var data = CreditoBL.ReporteClientesInactivos(pOficinaId, pUsuarioId, DateTime.Parse(pFechaIni), DateTime.Parse(pFechaFin));
            var rd = new ReportDataSource("dsClienteInactivo", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;
            var agente = "TODOS";
            if (pUsuarioId != null)
                agente = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioId.Value, includeProperties: "Persona").Persona.NombreCompleto;

            var totalStr = (data != null) ? data.Count.ToString() : "0";

            var parametros = new List<ReportParameter>
    {
        new ReportParameter("Fecha", " DEL " + pFechaIni + " AL " + pFechaFin),
        new ReportParameter("Oficina", oficina),
        new ReportParameter("Agente", agente),
        
        new ReportParameter("TotalClientesInactivos", totalStr)
    };

            return Reporte(pTipo, "rptClienteInactivo.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteCreditoObservado(int? pOficinaId, int? pUsuarioId, string pTipo = "PDF")
        {
            var data = CreditoBL.ReporteCreditoObservado(pOficinaId, pUsuarioId);
            var rd = new ReportDataSource("dsCreditoObservado", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;
            var agente = "TODOS";
            if (pUsuarioId != null)
                agente = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioId.Value, includeProperties: "Persona").Persona.NombreCompleto;


            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina", oficina),
                                     new ReportParameter("Agente", agente),
                                      new ReportParameter("Titulo", "Creditos Observados")
                                 };
            return Reporte(pTipo, "rptCreditoObservado.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteCreditoCondonado(int? pOficinaId, int? pUsuarioId,
                                                string pFechaIni, string pFechaFin, string pTipo = "PDF")
        {
            var data = CreditoBL.ReporteCreditoCondonado(pOficinaId, pUsuarioId, DateTime.Parse(pFechaIni), DateTime.Parse(pFechaFin));
            var rd = new ReportDataSource("dsCreditoCondonado", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;
            var agente = "TODOS";
            if (pUsuarioId != null)
                agente = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioId.Value, includeProperties: "Persona").Persona.NombreCompleto;


            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina", oficina),
                                     new ReportParameter("Agente", agente),
                                      new ReportParameter("FechaInicio", DateTime.Parse(pFechaIni).ToShortDateString()),
                                       new ReportParameter("FechaFin", DateTime.Parse(pFechaFin).ToShortDateString())
                                 };
            return Reporte(pTipo, "rptCreditoCondonado.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteCreditoMorosoPagado(int? pOficinaId, int? pUsuarioId,
            string pFechaIni, string pFechaFin, string pTipo = "PDF")
        {
            var data = CreditoBL.ReporteCreditoMorosoPagado(pOficinaId, pUsuarioId, DateTime.Parse(pFechaIni), DateTime.Parse(pFechaFin));
            var rd = new ReportDataSource("dsCreditosMorososPagados", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;
            var agente = "TODOS";
            if (pUsuarioId != null)
                agente = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioId.Value, includeProperties: "Persona").Persona.NombreCompleto;


            var parametros = new List<ReportParameter>
            {
                new ReportParameter("Oficina", oficina),
                new ReportParameter("Agente", agente),
                new ReportParameter("FechaInicio", DateTime.Parse(pFechaIni).ToShortDateString()),
                new ReportParameter("FechaFin", DateTime.Parse(pFechaFin).ToShortDateString())
            };
            return Reporte(pTipo, "rptCreditosMorososPagados.rdlc", rd, "A4Horizontal0.25", parametros);
        }

        public ActionResult ReporteSaldoCarteraCajaDiario(int? pUsuarioId, int? pOficinaId, int anioIni, int mesIni, int anioFin, int mesFin)
        {
            var data = CajaDiarioBL.ReporteSaldoCarteraCajaDiario(pUsuarioId, pOficinaId, anioIni, mesIni, anioFin, mesFin);
            var rd = new ReportDataSource("dsSaldoCarteraCajaDiario", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;
            var agente = "TODOS";
            if (pUsuarioId != null)
                agente = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioId.Value, includeProperties: "Persona").Persona.NombreCompleto;


            var parametros = new List<ReportParameter>
            {
                new ReportParameter("Oficina", oficina),
                new ReportParameter("Agente", agente),
                new ReportParameter("PeriodoInicio", VendixGlobal.GetNombreMes(mesIni) +" "+ anioIni.ToString()),
                new ReportParameter("PeriodoFin",VendixGlobal.GetNombreMes(mesFin) +" "+ anioFin.ToString())
            };
            return Reporte("Excel", "rptSaldoCarteraCajaDiario.rdlc", rd, "A4Horizontal0.25", parametros);
        }

        public ActionResult ReporteCreditoActivo(int? pOficinaId, int? pUsuarioId,
                                               string pFechaIni, string pFechaFin, string pTipo = "PDF")
        {
            var data = CreditoBL.ReporteCreditoActivo(pOficinaId, pUsuarioId, DateTime.Parse(pFechaIni), DateTime.Parse(pFechaFin));
            var rd = new ReportDataSource("dsCreditoActivo", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;
            var agente = "TODOS";
            if (pUsuarioId != null)
                agente = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioId.Value, includeProperties: "Persona").Persona.NombreCompleto;


            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina", oficina),
                                     new ReportParameter("Agente", agente),
                                      new ReportParameter("FechaInicio", DateTime.Parse(pFechaIni).ToShortDateString()),
                                       new ReportParameter("FechaFin", DateTime.Parse(pFechaFin).ToShortDateString()),
                                       new ReportParameter("Desembolsos", data.Count(x=>x.Estado=="DESEMBOLSADO").ToString()),
                                       new ReportParameter("Pagados", data.Count(x=>x.Estado=="PAGADO").ToString())
                                 };
            return Reporte(pTipo, "rptCreditoActivo.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteCreditoCierre(int? pOficinaId, int? pUsuarioId,
            string pFechaIni, string pFechaFin, string pTipo = "PDF")
        {
            var data = CreditoBL.ReporteCreditoCierre(pOficinaId, pUsuarioId, DateTime.Parse(pFechaIni), DateTime.Parse(pFechaFin));
            var rd = new ReportDataSource("dsCreditoCierre", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;
            var agente = "TODOS";
            if (pUsuarioId != null)
                agente = UsuarioBL.Obtener(x => x.UsuarioId == pUsuarioId.Value, includeProperties: "Persona").Persona.NombreCompleto;


            var parametros = new List<ReportParameter>
            {
                new ReportParameter("FechaInicio", DateTime.Parse(pFechaIni).ToShortDateString()),
                new ReportParameter("FechaFin", DateTime.Parse(pFechaFin).ToShortDateString()),
                new ReportParameter("Oficina", oficina),
                new ReportParameter("Agente", agente)
            };
            return Reporte(pTipo, "rptCreditoCierre.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteMorosidadGestor(int? pOficinaId, int? pUsuarioId, string pTipo = "PDF")
        {
            return ReporteCobroDiario(pUsuarioId, pOficinaId, pTipo, true);
        }
        public ActionResult ReporteAvanceVenta(string pFechaIni, string pFechaFin, bool indContado, bool indCredito, int? pOficinaId)
        {
            var data = ReporteBL.ListarReporteRentabilidadVenta(DateTime.Parse(pFechaIni), DateTime.Parse(pFechaFin), indContado, indCredito, pOficinaId);
            var rd = new ReportDataSource("dsRentabilidad", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;

            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina", oficina),
                                     new ReportParameter("FechaIni", DateTime.Parse(pFechaIni).ToShortDateString()),
                                     new ReportParameter("FechaFin", DateTime.Parse(pFechaFin).ToShortDateString())
                                 };
            return Reporte("PDF", "rptRentabilidad.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteListaPrecio(int? pMarcaId, bool pIndDescuento, bool pIndPuntos, string pTipo = "PDF")
        {
            var data = ReporteBL.ListarReporteListaPrecio(pMarcaId, pIndDescuento, pIndPuntos);
            var rd = new ReportDataSource("dsListaPrecio", data);
            var marca = "TODOS";
            if (pMarcaId != null)
                marca = MarcaBL.Obtener(pMarcaId.Value).Denominacion;

            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Marca", marca),
                                     new ReportParameter("EsDescuento", pIndDescuento?"SI":"NO"),
                                     new ReportParameter("EsPunto", pIndPuntos?"SI":"NO")
                                 };
            return Reporte(pTipo, "rptListaPrecio.rdlc", rd, "A4Vertical0.25", parametros);
        }
        public ActionResult ReporteStock(int? pOficinaId, string pTipoReporte = "PDF")
        {
            var data = ReporteBL.ListarReporteStockGeneral(pOficinaId);
            var rd = new ReportDataSource("dsStock", data);

            var oficina = "TODOS";
            if (pOficinaId != null)
                oficina = OficinaBL.Obtener(pOficinaId.Value).Denominacion;

            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina", oficina)
                                 };
            //return Reporte("Excel", "rptStock.rdlc", rd, "A4Vertical0.25", parametros);
            return Reporte(pTipoReporte, "rptStock.rdlc", rd, "A4Horizontal0.25", parametros);
        }
        public ActionResult ReporteStockAnulados(string pTipoReporte = "PDF")
        {
            var data = ReporteBL.ListarReporteStockAnulados();
            var rd = new ReportDataSource("dsStokAnulados", data);

            //var parametros = new List<ReportParameter>
            //                     {
            //                         new ReportParameter("Oficina", oficina)
            //                     };
            //return Reporte("Excel", "rptStock.rdlc", rd, "A4Vertical0.25", parametros);
            return Reporte(pTipoReporte, "rptStockAnulados.rdlc", rd, "A4Horizontal0.25", null);
        }
        public ActionResult ReporteSaldoCajaActual()
        {
            return ReporteSaldoCaja(VendixGlobal.GetCajaDiarioId());
        }
        public ActionResult ReporteSaldoCaja(int pCajaDiarioId)
        {
            var data = CajaDiarioBL.ReporteSaldoCajaDiario(pCajaDiarioId);
            if (data.Count == 0)
                return Content("Reporte Sin Registros!!!!");
            var rd = new ReportDataSource("dsSaldoCaja", data);
            var cab = CajaDiarioBL.ObtenerRptSaldoCajaCab(pCajaDiarioId);
            var resumeningreso = CajaDiarioBL.ObtenerResumenIngresoCajaDiario(pCajaDiarioId);

            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina",cab.Oficina ),
                                     new ReportParameter("Cajero",cab.Cajero ),
                                     new ReportParameter("Estado",cab.Estado),
                                     new ReportParameter("Fecha",cab.Fecha.ToShortDateString() ),
                                     new ReportParameter("SaldoInicial",cab.SaldoInicial.ToString() ),
                                     new ReportParameter("SaldoFinal",cab.SaldoFinal.ToString()),
                                     new ReportParameter("PorcentajeCobro",cab.PorcentajeCobro.ToString()),
                                     new ReportParameter("ResumenIngreso",resumeningreso)
                                 };
            return Reporte("PDF", "rptSaldoCaja.rdlc", rd, "A4Vertical0.25", parametros);

        }
        public ActionResult ReporteSaldoCajaChica(int pCajaChicaDiarioId)
        {
            var data = CajaChicaDiarioBL.ReporteSaldoCajaChicaDiario(pCajaChicaDiarioId);
            if (data.Count == 0)
                return Content("Reporte Sin Registros!!!!");
            var rd = new ReportDataSource("dsSaldoCaja", data);
            var cab = CajaChicaDiarioBL.ObtenerRptSaldoCajaChicaCab(pCajaChicaDiarioId);

            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("Oficina",cab.Oficina ),
                                     new ReportParameter("Cajero",cab.Cajero ),
                                     new ReportParameter("Estado",cab.Estado),
                                     new ReportParameter("Fecha",cab.Fecha.ToShortDateString() ),
                                     new ReportParameter("SaldoInicial",cab.SaldoInicial.ToString() ),
                                     new ReportParameter("SaldoFinal",cab.SaldoFinal.ToString()),
                                     new ReportParameter("PorcentajeCobro","0")
                                 };
            return Reporte("PDF", "rptSaldoCaja.rdlc", rd, "A4Vertical0.25", parametros);

        }
        public ActionResult ReporteCreditoMovimiento(int pCreditoId)
        {
            // 1. Obtenemos el crédito con sus relaciones necesarias (Quitamos ,Desembolso)
            var credito = CreditoBL.Obtener(x => x.CreditoId == pCreditoId, "Persona,Persona.Cliente,Aprobacion");

            // 2. Recuperar los datos del Aval
            Persona aval = null;
            if (credito.PersonaAvalId.HasValue)
            {
                aval = PersonaBL.Obtener(credito.PersonaAvalId.Value);
            }

            // 3. Obtener el nombre del Analista
            string analistaNombre = UsuarioBL.ObtenerNombre(credito.UsuarioRegId);

            // 4. Obtener la información de Aprobación limpia (MÉTODO CORREGIDO)
            string aprobadoPorInfo = "-";
            string fechaAprobacionTexto = "-"; // <- Nueva variable
            if (credito.Aprobacion != null && credito.Aprobacion.Any())
            {
                var apro = credito.Aprobacion.OrderByDescending(x => x.Nivel).FirstOrDefault();
                if (apro != null)
                {
                    // Extraemos la fecha del registro de aprobación hallado
                    fechaAprobacionTexto = apro.Fecha.HasValue ? apro.Fecha.Value.ToString("dd/MM/yyyy") : "-";

                    if (apro.UsuarioId.HasValue)
                    {
                        string usuarioAproNombre = UsuarioBL.ObtenerNombre(apro.UsuarioId.Value);
                        aprobadoPorInfo = usuarioAproNombre.Trim();
                    }
                }
            }

            // 5. Obtener la Actividad Económica
            string actividadEconTexto = "-";
            var cliente = credito.Persona.Cliente.FirstOrDefault();
            if (cliente != null && cliente.ActividadEconId.HasValue)
            {
                var ocupacion = OcupacionBL.Obtener(x => x.OcupacionId == cliente.ActividadEconId.Value);
                if (ocupacion != null)
                {
                    actividadEconTexto = ocupacion.Denominacion;
                }
            }

            // 6. CANTIDAD DE CRÉDITOS Y TOPE DE CRÉDITO (CON VALIDACIÓN DE ASIGNACIÓN)
            int totalCreditos = CreditoBL.Contar(x => x.PersonaId == credito.PersonaId && x.Estado != "CRE" && x.OficinaId == credito.OficinaId);

            // MODIFICACIÓN DEFINITIVA: Validamos si tiene un tope real asignado mayor a 0
            string topeCreditoTexto = (cliente != null && cliente.TopeCredito.HasValue && cliente.TopeCredito.Value > 0)
                ? cliente.TopeCredito.Value.ToString("N2")
                : "NO ASIGNADO";

            // 7. LÓGICA DE MOVIMIENTOS Y CÁLCULO DE TIEMPO REAL (MÉTODO HISTÓRICO SIN DUPLICADOS)
            string fechaUltimoPagoTexto = "-";
            var fechaActual = VendixGlobal.GetFecha().Date;

            // Usamos un HashSet para registrar los días calendario únicos en mora y evitar el efecto bola de nieve
            HashSet<DateTime> diasEnMoraUnicos = new HashSet<DateTime>();
            var todasLasCuotas = PlanPagoBL.Listar(x => x.CreditoId == pCreditoId);

            foreach (var cuota in todasLasCuotas)
            {
                if (cuota.Estado == "PAG")
                {
                    // Cuotas pagadas tarde: registramos el rango de días reales que estuvo en mora
                    if (cuota.MovimientoCajaId.HasValue)
                    {
                        var movPago = MovimientoCajaBL.Obtener(cuota.MovimientoCajaId.Value);
                        if (movPago != null && movPago.FechaReg.Date > cuota.FechaVencimiento.Date)
                        {
                            for (DateTime dia = cuota.FechaVencimiento.Date.AddDays(1); dia <= movPago.FechaReg.Date; dia = dia.AddDays(1))
                            {
                                diasEnMoraUnicos.Add(dia);
                            }
                        }
                    }
                }
                else
                {
                    // Cuotas vencidas actualmente: registramos los días desde el vencimiento hasta hoy
                    if (fechaActual > cuota.FechaVencimiento.Date)
                    {
                        for (DateTime dia = cuota.FechaVencimiento.Date.AddDays(1); dia <= fechaActual; dia = dia.AddDays(1))
                        {
                            diasEnMoraUnicos.Add(dia);
                        }
                    }
                }
            }

            // El total de días reales es el conteo de elementos únicos en nuestro conjunto
            int totalDiasRetraso = diasEnMoraUnicos.Count;










            // ====================================================================================
            // 8. LÓGICA DE ESTADOS Y RELLENO DE DÍAS SIN PAGO
            // ====================================================================================
            var oMovOriginal =
                CreditoBL.ReporteCreditoMovimiento(pCreditoId)
                ?? new List<usp_RptMovimientoCredito_Result>();

            // Solamente consideramos pagos con importe mayor que cero.
            var pagosReales = oMovOriginal
                .Where(x =>
                    x.Fecha.HasValue &&
                    (x.ImportePago ?? 0m) > 0m)
                .OrderByDescending(x => x.Fecha.Value)
                .ToList();

            // Debido al orden descendente, el primero es el pago más reciente.
            var ultimoMovimientoPagado = pagosReales.FirstOrDefault();

            string ultimaFechaPagada = ultimoMovimientoPagado != null
                ? ultimoMovimientoPagado.Fecha.Value.ToString("dd/MM/yyyy")
                : "-";

            var Saldototal = oMovOriginal.Any()
                ? oMovOriginal.First().Saldo
                : 0;

            var oMov = new List<usp_RptMovimientoCredito_Result>();


















            bool esCreditoDiario =
    !string.IsNullOrWhiteSpace(credito.FormaPago) &&
    (
        credito.FormaPago.Equals(
            "D",
            StringComparison.OrdinalIgnoreCase) ||
        credito.FormaPago.IndexOf(
            "DIARIO",
            StringComparison.OrdinalIgnoreCase) >= 0
    );

            if (oMovOriginal.Any() && esCreditoDiario)
            {
                DateTime fechaInicio = credito.FechaPrimerPago.Date;
                DateTime fechaFin = credito.FechaVencimiento.Date;

                // El reporte se genera como máximo hasta la fecha actual.
                if (fechaFin > fechaActual)
                {
                    fechaFin = fechaActual;
                }

                // Solo extendemos la fecha final si existe un pago verdadero.
                if (ultimoMovimientoPagado != null)
                {
                    DateTime fechaUltimoPagoReal =
                        ultimoMovimientoPagado.Fecha.Value.Date;

                    if (fechaUltimoPagoReal > fechaFin)
                    {
                        fechaFin = fechaUltimoPagoReal;
                    }
                }

                decimal saldoArrastrado =
                    Convert.ToDecimal(oMovOriginal.First().Saldo) +
                    Convert.ToDecimal(oMovOriginal.First().ImportePago);

                for (DateTime dia = fechaInicio;
                     dia <= fechaFin;
                     dia = dia.AddDays(1))
                {
                    if (dia.DayOfWeek == DayOfWeek.Sunday)
                    {
                        continue;
                    }

                    var pagosDelDia = oMovOriginal
                        .Where(x =>
                            x.Fecha.HasValue &&
                            x.Fecha.Value.Date == dia)
                        .ToList();

                    if (pagosDelDia.Any())
                    {
                        // Cuando hay un pago real, eliminamos los registros
                        // de importe cero correspondientes al mismo día.
                        if (pagosDelDia.Any(x => (x.ImportePago ?? 0m) > 0m))
                        {
                            pagosDelDia = pagosDelDia
                                .Where(x => (x.ImportePago ?? 0m) > 0m)
                                .ToList();
                        }

                        foreach (var movimiento in pagosDelDia)
                        {
                            oMov.Add(movimiento);
                            saldoArrastrado =
                                Convert.ToDecimal(movimiento.Saldo);
                        }
                    }
                    else
                    {
                        oMov.Add(new usp_RptMovimientoCredito_Result
                        {
                            Fecha = dia,
                            ImportePago = 0.00m,
                            Saldo = saldoArrastrado
                        });
                    }
                }
            }
            else
            {
                // Créditos semanales, quincenales o mensuales.
                oMov = oMovOriginal.ToList();

                // Retiramos el desembolso cuando no representa un pago.
                if (credito.FechaDesembolso.HasValue)
                {
                    oMov = oMov
                        .Where(x =>
                            !(
                                (x.ImportePago ?? 0m) == 0m &&
                                x.Fecha.HasValue &&
                                x.Fecha.Value.Date ==
                                    credito.FechaDesembolso.Value.Date
                            ))
                        .ToList();
                }
            }








            if (esCreditoDiario)
            {
                totalDiasRetraso = oMov.Count(x =>
                    (x.ImportePago ?? 0m) == 0m);
            }
            else
            {
                var cuotasPendientes = PlanPagoBL.Listar(x =>
                    x.CreditoId == pCreditoId &&
                    x.Estado != "PAG");

                if (cuotasPendientes != null && cuotasPendientes.Any())
                {
                    DateTime vencimientoPendienteMasAntiguo = cuotasPendientes
                        .Min(x => x.FechaVencimiento.Date);

                    if (fechaActual > vencimientoPendienteMasAntiguo)
                    {
                        totalDiasRetraso = 0;

                        for (
                            DateTime dia = vencimientoPendienteMasAntiguo.AddDays(1);
                            dia <= fechaActual;
                            dia = dia.AddDays(1))
                        {
                            if (dia.DayOfWeek != DayOfWeek.Sunday)
                            {
                                totalDiasRetraso++;
                            }
                        }
                    }
                    else
                    {
                        // Existen cuotas pendientes, pero todavía no están vencidas.
                        totalDiasRetraso = 0;
                    }
                }
                else
                {
                    // No existen cuotas pendientes.
                    totalDiasRetraso = 0;
                }
            }









            var rd = new ReportDataSource("dsCreditoMov", oMov);
            string estadoCredito = string.Empty;

            // Lógica para determinar el estado de la cabecera
            if (credito.Estado == "PAG")
            {
                estadoCredito = credito.IndCondonacion ? "CANCELADO CONDONADO" : "CANCELADO PAGADO";
            }
            else if (credito.Estado == "ANU")
            {
                estadoCredito = "ANULADO";
            }
            else if (credito.Estado == "CRE")
            {
                estadoCredito = "SOLICITUD DE CREDITO";
            }
            else if (credito.Estado == "DES" || credito.Estado == "PEN" || credito.Estado == "REP")
            {
                if (fechaActual > credito.FechaVencimiento.Date)
                {
                    estadoCredito = "VENCIDO";
                }
                else
                {
                    estadoCredito = "VIGENTE";
                }
            }
            else
            {
                estadoCredito = credito.Estado;
            }

            // EXTRACCIÓN DE MORA REUTILIZANDO LA LÓGICA DEL COBRO DIARIO
            decimal saldoMoraValue = 0m;

            // Limitamos la consulta al analista y oficina del crédito.
            // Antes se enviaba null como usuario, procesando toda la oficina.
            var listaCobroDiario =
                CreditoBL.ReporteCobroDiario(
                    credito.UsuarioRegId,
                    credito.OficinaId);

            if (listaCobroDiario != null)
            {
                var infoCreditoActual = listaCobroDiario.FirstOrDefault(
                    x => x.CreditoId == pCreditoId);

                if (infoCreditoActual != null)
                {
                    saldoMoraValue = infoCreditoActual.Mora ?? 0m;
                }
            }

            // 8.1. Obtener la fecha de Condonación exacta para validación de cierre de caja
            string fechaCondonacionTexto = "-";
            if (credito.Estado == "PAG" && credito.IndCondonacion)
            {
                if (oMovOriginal != null && oMovOriginal.Any())
                {
                    var ultimoMov = oMovOriginal.LastOrDefault();
                    if (ultimoMov != null && ultimoMov.Fecha.HasValue)
                    {
                        fechaCondonacionTexto = ultimoMov.Fecha.Value.ToString("dd/MM/yyyy");
                    }
                }
            }

            // 9. Inyección de la lista completa de parámetros al archivo RDLC
            var parametros = new List<ReportParameter>
         {
             new ReportParameter("CreditoId", pCreditoId.ToString()),
             new ReportParameter("Cliente", credito.Persona.NumeroDocumento + "-" + credito.Persona.NombreCompleto),
             new ReportParameter("FechaNacimiento", credito.Persona.FechaNacimiento.HasValue?credito.Persona.FechaNacimiento.Value.ToShortDateString():""),
             new ReportParameter("Celular", credito.Persona.Celular1 ),
             new ReportParameter("Direccion", credito.Persona.Direccion ),
             new ReportParameter("DireccionRef", credito.Persona.DireccionRef ),
             new ReportParameter("DireccionNegocio", cliente != null ? cliente.DireccionNegocio : "-"),
             new ReportParameter("DireccionNegocioRef", cliente != null ? cliente.DireccionNegocioRef : "-"),
             new ReportParameter("FormaPago", credito.FormaPago ),
             new ReportParameter("Cuotas", credito.NumeroCuotas.ToString() ),
             new ReportParameter("FechaPrimerPago", credito.FechaPrimerPago.DayOfWeek == DayOfWeek.Sunday
                ? credito.FechaPrimerPago.AddDays(1).ToShortDateString()
                : credito.FechaPrimerPago.ToShortDateString() ),
             new ReportParameter("FechaVencimiento", credito.FechaVencimiento.ToShortDateString() ),
             new ReportParameter("Estado", estadoCredito ),
             new ReportParameter("MontoTotal", Saldototal.ToString() ),
             new ReportParameter("MontoCredito", credito.MontoCredito.ToString() ),
             new ReportParameter("GA", credito.MontoGastosAdm.ToString() ),
             new ReportParameter("Interes", credito.Interes.ToString() ),
             new ReportParameter("Observacion", (credito.Observacion ?? "").ToString() ),

             // NUEVOS PARÁMETRO DE MORA
             new ReportParameter("SaldoMora", saldoMoraValue.ToString("N2")),

             // Parámetros del Aval
             new ReportParameter("AvalNombre", aval != null ? aval.NombreCompleto : "SIN AVAL"),
             new ReportParameter("AvalDni", aval != null ? aval.NumeroDocumento : "-"),
             new ReportParameter("AvalDireccion", aval != null ? (aval.Direccion!=null?aval.Direccion:"") : "-"),
             new ReportParameter("AvalCelular", aval != null ? aval.Celular1 : "-"),

             // Parámetros de Resumen y Auditoría
             new ReportParameter("Analista", !string.IsNullOrEmpty(analistaNombre) ? analistaNombre : "-"),
             new ReportParameter("AprobadoPor", aprobadoPorInfo),
             new ReportParameter("ActividadEconomica", actividadEconTexto),
             new ReportParameter("Calificacion", !string.IsNullOrEmpty(credito.Calificacion) ? credito.Calificacion.Trim() : "-"),
             new ReportParameter("TotalCreditos", totalCreditos.ToString()),
             new ReportParameter("TopeCredito", topeCreditoTexto),
             new ReportParameter("FechaUltimoPago", ultimaFechaPagada),
                             
             // Aquí se envía el cálculo matemático correcto de los días reales transcurridos
             new ReportParameter("DiasRetraso", totalDiasRetraso.ToString()),
             // ADICIÓN: Enviamos el nuevo parámetro exigido por tu rptCreditoMov.rdlc
             new ReportParameter("FechaAprobacion", fechaAprobacionTexto),
             new ReportParameter("FechaCondonacion", fechaCondonacionTexto)
         };

            return Reporte("PDF", "rptCreditoMov.rdlc", rd, "A4Vertical0.25", parametros);
        }
        public ActionResult ReporteCreditoVencido(string pVencidoMenor60, string pVencidoMayor60, string pVencidoIrrecuperable, string pTipoReporte = "Excel")
        {
            var fecha = VendixGlobal.GetFecha();
            if (pVencidoMenor60 == "null") pVencidoMenor60 = null;
            if (pVencidoMayor60 == "null") pVencidoMayor60 = null;
            if (pVencidoIrrecuperable == "null") pVencidoIrrecuperable = null;

            var obj = CreditoBL.ReporteCreditoVencido(pVencidoMenor60, pVencidoMayor60, pVencidoIrrecuperable);

            var rd = new ReportDataSource("dsReporteCreditoVencido", obj);

            var parametros = new List<ReportParameter>
                                 {
                                     new ReportParameter("FechaReporte", fecha.ToShortDateString())
                                 };

            return Reporte(pTipoReporte, "rptCreditoVencido.rdlc", rd, "A4Horizontal0.25", parametros);
        }

        public ActionResult ReporteCreditoTarea()
        {
            var fechaReporte = VendixGlobal.GetFecha().ToString("dd/MM/yyyy HH:mm");
            var tareas = TareaBL.ListarTareasParaReporte();
            var datosReporte = new List<TareaReporteData>();
            int contador = 1;
            foreach (var tarea in tareas)
            {
                var detalleSubtareas = "";
                if (tarea.Subtareas != null && tarea.Subtareas.Count > 0)
                {
                    var subtareasTexto = tarea.Subtareas.Select(s =>
                        (s.Completada ? "[Ok] " : "[X ] ") + s.Titulo);
                    detalleSubtareas = string.Join(Environment.NewLine, subtareasTexto);
                }

                datosReporte.Add(new TareaReporteData
                {
                    Nro = contador,
                    Cliente = tarea.ClienteCompleto,
                    Analista = tarea.NombreUsuario ?? "",
                    Subtareas = tarea.SubtareasTexto,
                    DetalleSubtareas = detalleSubtareas,
                    Estado = tarea.Estado
                });
                contador++;
            }
            var rd = new ReportDataSource("dsTareas", datosReporte);

            var parametros = new List<ReportParameter>
            {
                new ReportParameter("FechaReporte", fechaReporte),
                new ReportParameter("TotalTareas", tareas.Count.ToString())
            };

            return Reporte("PDF", "rptTareas.rdlc", rd, "A4Vertical0.25", parametros);
        }
        public ActionResult ReporteCobroDiario(
            int? pGestorid,
            int? pOficinaid,
            string pTipo = "PDF",
            bool indMora = false)
                {
                    var titulo = "COBRO DIARIO";
                    string agente;
                    string caja = string.Empty;

                    if (!indMora)
                    {
                        if (!pGestorid.HasValue)
                        {
                            pGestorid = VendixGlobal.GetUsuarioId();
                        }

                        var usuario = UsuarioBL.Obtener(
                            x => x.UsuarioId == pGestorid.Value,
                            "Persona");

                        agente = usuario.Persona.NombreCompleto;

                        var cajaUsuario = CajaBL.Obtener(
                            x => x.CajeroId == pGestorid.Value);

                        caja = cajaUsuario == null
                            ? string.Empty
                            : cajaUsuario.Denominacion;
                    }
                    else
                    {
                        titulo = "REPORTE DE MOROSIDAD";

                        if (pGestorid.HasValue)
                        {
                            var usuario = UsuarioBL.Obtener(
                                x => x.UsuarioId == pGestorid.Value,
                                "Persona");

                            agente = usuario.Persona.NombreCompleto;

                            var cajaUsuario = CajaBL.Obtener(
                                x => x.CajeroId == pGestorid.Value);

                            caja = cajaUsuario == null
                                ? string.Empty
                                : cajaUsuario.Denominacion;
                        }
                        else
                        {
                            agente = "TODOS";
                        }
                    }

                    // El procedimiento almacenado determina:
                    // - FechaPago
                    // - TienePagoReal
                    // No deben sobrescribirse en el controlador.
                    var oCredito = CreditoBL.ReporteCobroDiario(
                        pGestorid,
                        pOficinaid);

                    if (indMora)
                    {
                        oCredito = oCredito
                            .Where(x => x.Mora > 0)
                            .ToList();
                    }

                    var saldoMoroso = oCredito
                        .Where(x => x.Mora > 0)
                        .Sum(x => x.Saldo);

                    var saldoPendiente = oCredito.Sum(x => x.Saldo);

                    var rd = new ReportDataSource(
                        "dsCobroDiario",
                        oCredito);

                    var parametros = new List<ReportParameter>
            {
                new ReportParameter(
                    "Fecha",
                    VendixGlobal.GetFecha().ToString("dd/MM/yyyy")),

                new ReportParameter(
                    "Agente",
                    agente ?? string.Empty),

                new ReportParameter(
                    "Caja",
                    caja ?? string.Empty),

                new ReportParameter(
                    "SaldoVencido",
                    saldoPendiente.ToString()),

                new ReportParameter(
                    "SaldoMoroso",
                    saldoMoroso.ToString()),

                new ReportParameter(
                    "Titulo",
                    titulo),

                new ReportParameter(
                    "NroClientes",
                    oCredito.Count.ToString())
            };

            return Reporte(
                pTipo,
                "rptCobroDiario.rdlc",
                rd,
                "A4Horizontal0.25",
                parametros);
        }

        public FileContentResult ReporteCentrarRiegoTXT(int? pOficinaId, int pAnio, int pMes)
        {
            var rpt = ReporteBL.ListarReporteCentralRiesgo(pOficinaId, pAnio, pMes);
            int numeroItems = rpt.Count();
            string vacio = string.Empty;

            var sw = new StringWriter();
            using (sw)
            {
                for (int i = 0; i < numeroItems; i++)
                {
                    sw.WriteLine(rpt[i].Periodo + rpt[i].Entidad + rpt[i].TipoDoc.ToString().PadLeft(31, ' ') + rpt[i].NumDoc.PadLeft(12, ' ')
                        + rpt[i].RazonSocial.PadLeft(100, ' ') + rpt[i].ApePat.PadRight(20, ' ') + rpt[i].ApeMat.PadRight(20, ' ') + rpt[i].Nombres.PadRight(30, ' ')
                        + rpt[i].TipoPersona + rpt[i].ModalidadCredito + rpt[i].DeudaMenor30.Replace(".", "").PadLeft(39, ' ') + rpt[i].DeudaMayor30.Replace(".", "").PadLeft(13, ' ')
                        + vacio.PadLeft(65, ' ') + vacio.PadLeft(117, ' ') + rpt[i].Calificacion + rpt[i].DiasAtrazo.ToString().PadRight(5, ' ')
                        + rpt[i].Direccion.PadRight(80, ' ') + vacio.PadLeft(120, ' ') + rpt[i].celular.PadRight(10, ' '));
                }
            }

            String NombreArchivo = "DM007898";
            return File(new System.Text.UTF8Encoding().GetBytes(sw.ToString()), "text/txt", NombreArchivo + ".txt");
        }

        public ActionResult Reporte(string pTipoReporte, string rdlc, ReportDataSource rds, string pPapel, List<ReportParameter> pParametros = null)
        {
            var lr = new LocalReport();
            lr.ReportPath = Path.Combine(Server.MapPath("~/Reporte"), rdlc);

            if (rds != null) lr.DataSources.Add(rds);
            if (pParametros != null) lr.SetParameters(pParametros);

            string reportType = pTipoReporte;
            string mimeType;
            string encoding;
            string fileNameExtension;

            var deviceInfo = ObtenerPapel(pPapel).Replace("[TipoReporte]", pTipoReporte);
            Warning[] warnings;
            string[] streams;

            byte[] renderedBytes = lr.Render(reportType, deviceInfo, out mimeType, out encoding,
                                             out fileNameExtension, out streams, out warnings);

            return File(renderedBytes, mimeType);
        }

        private static string ObtenerPapel(string pPapel)
        {
            switch (pPapel)
            {
                case "A4Horizontal":
                    return "<DeviceInfo>" +
                           "  <OutputFormat>[TipoReporte]</OutputFormat>" +
                           "  <PageWidth>11in</PageWidth>" +
                           "  <PageHeight>8.5in</PageHeight>" +
                           "  <MarginTop>0in</MarginTop>" +
                           "  <MarginLeft>0in</MarginLeft>" +
                           "  <MarginRight>0in</MarginRight>" +
                           "  <MarginBottom>0in</MarginBottom>" +
                           "</DeviceInfo>";
                case "A4Vertical":
                    return "<DeviceInfo>" +
                           "  <OutputFormat>[TipoReporte]</OutputFormat>" +
                           "  <PageWidth>8.5in</PageWidth>" +
                           "  <PageHeight>11in</PageHeight>" +
                           "  <MarginTop>0in</MarginTop>" +
                           "  <MarginLeft>0in</MarginLeft>" +
                           "  <MarginRight>0in</MarginRight>" +
                           "  <MarginBottom>0in</MarginBottom>" +
                           "</DeviceInfo>";
                case "A4Horizontal0.25":
                    return "<DeviceInfo>" +
                           "  <OutputFormat>[TipoReporte]</OutputFormat>" +
                           "  <PageWidth>11in</PageWidth>" +
                           "  <PageHeight>8.5in</PageHeight>" +
                           "  <MarginTop>0.25in</MarginTop>" +
                           "  <MarginLeft>0.25in</MarginLeft>" +
                           "  <MarginRight>0.25in</MarginRight>" +
                           "  <MarginBottom>0.25in</MarginBottom>" +
                           "</DeviceInfo>";
                case "A4Vertical0.25":
                    return "<DeviceInfo>" +
                           "  <OutputFormat>[TipoReporte]</OutputFormat>" +
                           "  <PageWidth>8.5in</PageWidth>" +
                           "  <PageHeight>11in</PageHeight>" +
                           "  <MarginTop>0.25in</MarginTop>" +
                           "  <MarginLeft>0.25in</MarginLeft>" +
                           "  <MarginRight>0.25in</MarginRight>" +
                           "  <MarginBottom>0.25in</MarginBottom>" +
                           "</DeviceInfo>";
                case "TicketCaja":
                    return "<DeviceInfo>" +
                           "  <OutputFormat>[TipoReporte]</OutputFormat>" +
                           "  <PageWidth>3in</PageWidth>" +
                           "  <PageHeight>5.0in</PageHeight>" +
                           "  <MarginTop>0in</MarginTop>" +
                           "  <MarginLeft>0in</MarginLeft>" +
                           "  <MarginRight>0in</MarginRight>" +
                           "  <MarginBottom>0in</MarginBottom>" +
                           "</DeviceInfo>";
                case "VoucherCaja":
                    return "<DeviceInfo>" +
                           "  <OutputFormat>[TipoReporte]</OutputFormat>" +
                           "  <PageWidth>8.5in</PageWidth>" +
                           "  <PageHeight>11in</PageHeight>" +
                           "  <MarginTop>0in</MarginTop>" +
                           "  <MarginLeft>0in</MarginLeft>" +
                           "  <MarginRight>0in</MarginRight>" +
                           "  <MarginBottom>0in</MarginBottom>" +
                           "</DeviceInfo>";
                case "CodigoBarras":
                    return "<DeviceInfo>" +
                           "  <OutputFormat>[TipoReporte]</OutputFormat>" +
                           "  <PageWidth>4.13in</PageWidth>" +
                           "  <PageHeight>2.76in</PageHeight>" +
                           "  <MarginTop>0in</MarginTop>" +
                           "  <MarginLeft>0in</MarginLeft>" +
                           "  <MarginRight>0in</MarginRight>" +
                           "  <MarginBottom>0in</MarginBottom>" +
                           "</DeviceInfo>";

            }

            return "<DeviceInfo>" +
                   "  <OutputFormat>[TipoReporte]</OutputFormat>" +
                   "  <PageWidth>8.5in</PageWidth>" +
                   "  <PageHeight>11in</PageHeight>" +
                   "  <MarginTop>0in</MarginTop>" +
                   "  <MarginLeft>0in</MarginLeft>" +
                   "  <MarginRight>0in</MarginRight>" +
                   "  <MarginBottom>0in</MarginBottom>" +
                   "</DeviceInfo>";
        }
        #endregion

    }
}
