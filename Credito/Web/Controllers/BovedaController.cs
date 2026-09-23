using Helper;
using ITB.VENDIX.BL;
using ITB.VENDIX.DA;
using System;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.Linq;
using System.Web.Mvc;

namespace VendixWeb.Controllers
{
    [Autenticado]
    public class BovedaController : Controller
    {

        public ActionResult Index()
        {
            // 1. Unificamos la variable de oficina en una sola para todo el método
            var oficinaId = VendixGlobal.GetOficinaId();

            ViewBag.cboCajas = new SelectList(CajaBL.ListarCajasAbiertas(), "id", "value");
            ViewBag.cboOficinas = new SelectList(OficinaBL.Listar(x => x.Estado && x.OficinaId != oficinaId), "OficinaId", "Denominacion");
            ViewBag.cboTipoOperacion = new SelectList(TipoOperacionBL.Listar(x => x.IndBoveda, x => x.OrderBy(y => y.Denominacion)), "TipoOperacionId", "Denominacion");
            ViewBag.cboUsuario = new SelectList(UsuarioBL.Listar(x => x.Estado && x.NombreUsuario != "ADMVENDIX", x => x.OrderBy(y => y.NombreUsuario)), "UsuarioId", "NombreUsuario");

            // Combo general de cuentas (Mantiene todo)
            ViewBag.cboTipoCuenta = new SelectList(ValorTablaBL.Listar(x => x.TablaId == 13 && x.ItemId > 0), "ItemId", "Denominacion");

            // SOLUCIÓN PARA LOS OTROS MODALES: Combo dedicado exclusivo para transferencias entre bancos
            // Filtra excluyendo el Efectivo (ID 1) y abarca de forma dinámica hasta tus nuevos bancos (ID 2 al 8)
            var bancosParaTransferencia = ValorTablaBL.Listar(x => x.TablaId == 13 && x.ItemId > 1).OrderBy(x => x.ItemId).ToList();
            ViewBag.cboBancosTransferencia = new SelectList(bancosParaTransferencia, "ItemId", "Denominacion");

            // ====================================================================
            // FILTRO DE SEGURIDAD: SOLO MOSTRAR ANALISTAS CON CAJA DIARIA ABIERTA
            // ====================================================================
            using (var db = new VENDIXEntities())
            {
                var analistasActivosConCaja = db.CajaDiario
                    .Where(c => c.IndCierre == false && c.Caja.OficinaId == oficinaId)
                    .Select(c => c.Usuario)
                    .Where(u => u.Estado == true)
                    .Distinct()
                    .OrderBy(u => u.NombreUsuario)
                    .ToList();

                ViewBag.cboAnalistas = new SelectList(analistasActivosConCaja, "UsuarioId", "NombreUsuario");
            }

            // Intenta obtener la bóveda abierta actual
            var oboveda = BovedaBL.Obtener(x => x.OficinaId == oficinaId && x.IndCierre == false);

            if (oboveda == null)
            {
                oboveda = new Boveda
                {
                    SaldoInicial = 0,
                    Entradas = 0,
                    Salidas = 0,
                    SaldoFinal = 0,
                    IndCierre = true,
                    FechaIniOperacion = DateTime.Now
                };
            }

            decimal dMontoCajaChica = 0, dMontoCajas = 0, dMontoPlanPagoPendiente = 0;

            var oCajaChica = CajaChicaDiarioBL.Obtener(x => x.IndCierre == false && x.TransBoveda == false);
            if (oCajaChica != null)
                dMontoCajaChica = oCajaChica.SaldoFinal;

            var oCajas = CajaDiarioBL.Listar(x => x.IndCierre == false && x.TransBoveda == false && x.Caja.OficinaId == oficinaId, includeProperties: "Caja");
            if (oCajas != null)
                dMontoCajas = oCajas.Sum(x => x.SaldoFinal);

            // REFUERZO: Si retorna null, le asignamos 0 de forma segura sin colapsar
            dMontoPlanPagoPendiente = BovedaBL.MontoPendientePlanPago(oficinaId) ?? 0;
            var creditoVencido = BovedaBL.CreditoVencido(null);

            ViewBag.MontoCajachica = dMontoCajaChica;
            ViewBag.MontoCajas = dMontoCajas;
            ViewBag.MontoPlanPagoPendiente = dMontoPlanPagoPendiente;
            ViewBag.MontoVencido = creditoVencido.CreditoVencido;
            ViewBag.VencidoMenor60 = creditoVencido.VencidoMenor60;
            ViewBag.VencidoMayor60 = creditoVencido.VencidoMayor60;
            ViewBag.VencidoIrrecuperable = creditoVencido.VencidoIrrecuperable;
            ViewBag.TotalFondo = (dMontoCajaChica + dMontoCajas + oboveda.SaldoFinal + dMontoPlanPagoPendiente + creditoVencido.CreditoVencido);

            var resumen = BovedaBL.ResumenCuentaBoveda();
            ViewBag.ResumenCuentaBoveda = resumen;

            return View(oboveda);
        }

        public ActionResult ListarBovedaJgrid(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = BovedaBL.LstBovedaJGrid(request, ref totalRecords);
            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstItem
                        select new
                        {
                            id = item.BovedaId,
                            cell = new string[] {
                                item.BovedaId.ToString(),
                                item.IndTemporal?"TMP":"PRI",
                                item.SaldoInicial.ToString(),
                                item.Entradas.ToString(),
                                item.Salidas.ToString(),
                                item.SaldoFinal.ToString(),
                                item.FechaIniOperacion.ToString(),
                                item.FechaFinOperacion.HasValue?item.FechaFinOperacion.Value.ToString():"",
                                item.IndCierre?"SI":"NO",
                                item.BovedaId.ToString()
                            }
                        }
                    ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarBovedaMovJgrid(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = BovedaBL.LstBovedaMovJGrid(request, ref totalRecords);

            // OPTIMIZACIÓN: Añadimos .ToList() para cargar los nombres de pago en memoria una sola vez
            var listaTipoPago = ValorTablaBL.Listar(x => x.TablaId == 13 && x.ItemId > 0).ToList();

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstItem
                        select new
                        {
                            id = item.MovimientoBovedaId,
                            cell = new string[] {
                                            item.MovimientoBovedaId.ToString(),
                                            item.BovedaId.ToString(),
                                            item.CajaDiarioId.ToString(),
                                            item.FechaReg.ToString(),
                                            item.CodOperacion,
                                            listaTipoPago.FirstOrDefault(x => x.ItemId == item.TipoPagoId)?.Denominacion ?? "CONSOLIDADO",
                                            item.Glosa,
                                            item.Importe.ToString(),
                                            item.MovimientoBovedaId.ToString()
                                        }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }        
        public ActionResult Cerrar()
        {
            return Json(BovedaBL.Cerrar(), JsonRequestBehavior.AllowGet);
        }
        public ActionResult CerrarBovedaTemporal()
        {
            return Json(BovedaBL.CerrarBovedaTemporal(), JsonRequestBehavior.AllowGet);
        }
        public ActionResult IngresoEgreso(decimal pImporte, string pDescripcion, int pTipoOperacionId, short pTipoCuentaId)
        {
            var rspta = BovedaMovBL.IngresoEgresoBovedaCaja(pImporte, pDescripcion, pTipoOperacionId, pTipoCuentaId);

            return Json(rspta, JsonRequestBehavior.AllowGet);
        }
        public ActionResult AsignarBovedaTemporal(decimal pImporte, string pDescripcion, int pUsuarioId)
        {
            var oficinaId = VendixGlobal.GetOficinaId();
            string mensaje = string.Empty;
            try
            {
                if (pUsuarioId > 0) // crea un boveda temporal a un usuario con ROL ENCARGADO
                {
                    var bovedatemporal = BovedaBL.Contar(x => x.OficinaId == oficinaId && x.IndCierre == false && x.IndTemporal == true);
                    if (bovedatemporal > 0)
                        return Json("Ya Existe una boveda Temporal Creada", JsonRequestBehavior.AllowGet);

                    BovedaMovBL.AsignarBovedaTemporal(pImporte, pDescripcion, pUsuarioId);
                }
                else
                { // transferir a boveda temporal
                    var bovedatemporal = BovedaBL.Contar(x => x.OficinaId == oficinaId && x.IndCierre == false && x.IndTemporal == true);
                    if (bovedatemporal == 0)
                        return Json("NO Existe una boveda Temporal Creada", JsonRequestBehavior.AllowGet);

                    BovedaMovBL.Tranferir_a_BovedaTemporal(pImporte, pDescripcion);
                }
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
            }

            return Json(mensaje, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ExisteBovedaTemporal()
        {
            var oficinaId = VendixGlobal.GetOficinaId();


            var bovedatemporal = BovedaBL.Contar(x => x.OficinaId == oficinaId && x.IndCierre == false && x.IndTemporal == true);
            if (bovedatemporal > 0)
                return Json(true, JsonRequestBehavior.AllowGet);

            return Json(false, JsonRequestBehavior.AllowGet);
        }

        public ActionResult TransferirCaja(decimal pImporte, string pDescripcion, int pCboId)
        {
            var rspta = BovedaMovBL.TransferirBovedaCaja(pImporte, pDescripcion, pCboId);
            return Json(rspta, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ResumenCuentaBoveda()
        {
            var rspta = BovedaBL.ResumenCuentaBoveda();
            return Json(rspta, JsonRequestBehavior.AllowGet);
        }

        public ActionResult TransferirCajaChica(decimal pImporte, string pDescripcion)
        {
            var rspta = BovedaMovBL.TransferirBovedaCajaChica(pImporte, pDescripcion);
            return Json(rspta, JsonRequestBehavior.AllowGet);
        }

        public ActionResult TransferirOficina(decimal pImporte, string pDescripcion, int pCboId)
        {
            var oficinaId = VendixGlobal.GetOficinaId();
            var bovedaActiva = BovedaBL.Listar(x => x.OficinaId == oficinaId && x.IndCierre == false).FirstOrDefault();
            if (bovedaActiva == null)
            {
                return Json("No se puede transferir: La Bóveda de origen se encuentra cerrada.", JsonRequestBehavior.AllowGet);
            }
            var bovedaInicioId = bovedaActiva.BovedaId;
            var pUsuarRegId = VendixGlobal.GetUsuarioId();

            var rpta = BovedaMovBL.TransferiraOficina(pImporte, pDescripcion, bovedaInicioId, pCboId, pUsuarRegId);

            return Json(rpta, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ConfirmarTransferencia(int pBovedaMovTempId, int pFlag)
        {
            var rpta = BovedaMovBL.TransferiraOficina(pBovedaMovTempId, pFlag);

            return Json(rpta, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ValidarCierre()
        {
            var oficinaid = VendixGlobal.GetOficinaId();
            if (CajaDiarioBL.Contar(x => x.IndCierre == false && x.Caja.OficinaId == oficinaid) > 0)
                return Json("EXISTEN CAJAS ABIERTAS.", JsonRequestBehavior.AllowGet);
            
            if (CajaDiarioBL.Contar(x => x.IndCierre==true && x.TransBoveda == false && x.Caja.OficinaId == oficinaid) > 0)
                return Json("EXISTEN CAJAS CERRADAS NO ENVIADAS A BOVEDA. REVISE FORMULARIO SALDOS CAJA.", JsonRequestBehavior.AllowGet);

            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult TransferirAAnalista(short pTipoPagoId, int pUsuarioAnalistaId, decimal pImporte, string pDescripcion)
        {
            try
            {
                // =================================================
                // 1. EXTRACCIÓN Y PARSEO DEL TEXTO PLANO DE SALDOS
                // =================================================
                var resumenSaldosRaw = BovedaBL.ResumenCuentaBoveda();
                decimal saldoDisponible = 0;

                if (!string.IsNullOrEmpty(resumenSaldosRaw))
                {
                    // Obtenemos el nombre oficial de la cuenta/banco (Denominacion) usando su ID
                    var oTipoPago = ValorTablaBL.Listar(x => x.TablaId == 13 && x.ItemId == pTipoPagoId).FirstOrDefault();

                    if (oTipoPago != null)
                    {
                        string nombreBanco = oTipoPago.Denominacion.ToUpper().Trim();
                        string buscar = nombreBanco + " =";
                        string textoSaldosUpper = resumenSaldosRaw.ToUpper();

                        int index = textoSaldosUpper.IndexOf(buscar);
                        if (index >= 0)
                        {
                            // Cortamos la cadena justo después del signo e igual " =" 
                            string desdeValor = resumenSaldosRaw.Substring(index + buscar.Length).Trim();

                            // Buscamos el siguiente espacio en blanco para aislar el número del banco
                            int espacioSiguiente = desdeValor.IndexOf(" ");
                            string valorStr = espacioSiguiente >= 0 ? desdeValor.Substring(0, espacioSiguiente) : desdeValor;

                            // Convertimos a decimal de forma segura (soporta negativos y formato invariable con punto)
                            decimal.TryParse(valorStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out saldoDisponible);
                        }
                    }
                }

                // Validaciones cuantitativas antes de proceder
                if (pImporte <= 0)
                {
                    return Json(new { success = false, message = "El importe debe ser mayor a cero." }, JsonRequestBehavior.AllowGet);
                }

                // CONDICIONAL DE CANTIDAD: Bloqueo inmediato si se quiere sobregirar el banco
                if (pImporte > saldoDisponible)
                {
                    return Json(new
                    {
                        success = false,
                        message = string.Format("Operación rechazada: Saldo insuficiente. El banco seleccionado solo dispone de {0:N2}.", saldoDisponible)
                    }, JsonRequestBehavior.AllowGet);
                }

                // ==========================================================
                // 2. LÓGICA DE NEGOCIO CON CONVERGENCIA A EFECTIVO (EMBUDO)
                // ==========================================================
                var oficinaId = VendixGlobal.GetOficinaId();
                var cajaDiario = CajaDiarioBL.Obtener(c => c.UsuarioAsignadoId == pUsuarioAnalistaId
                                                            && c.IndCierre == false
                                                            && c.Caja.OficinaId == oficinaId);

                if (cajaDiario == null)
                {
                    return Json(new { success = false, message = "El analista/agente seleccionado no tiene una caja abierta el día de hoy." }, JsonRequestBehavior.AllowGet);
                }

                // pTipoPagoId -> Resta al banco seleccionado de la Bóveda (Actualiza tus cards)
                // 1           -> Ingresa como EFECTIVO puro en la caja del analista (Tu Embudo)
                bool exito = BovedaMovBL.TransferirBovedaCaja(pImporte, pDescripcion, cajaDiario.CajaId, pTipoPagoId, 1);

                if (exito)
                {
                    return Json(new { success = true, message = "¡Transferencia realizada con éxito!" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "No se pudo registrar la transferencia en la base de datos." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error crítico en el servidor: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult RegistrarTransferenciaBancos(short tipoPagoOrigenId, short tipoPagoDestinoId, decimal importe, string glosa)
        {
            try
            {
                // =============================================================================
                // 1. REFUERZO DE SEGURIDAD: ID directo de Base de Datos (Evita Sesión Expirada)
                // =============================================================================
                int bovedaId = 0;
                int usuarioRegId = VendixGlobal.GetUsuarioId();
                var oficinaId = VendixGlobal.GetOficinaId();

                using (var db = new VENDIXEntities())
                {
                    // Buscamos la bóveda viva y abierta en tiempo real para esta oficina
                    var bovedaActiva = db.Boveda.FirstOrDefault(x => x.OficinaId == oficinaId && x.IndCierre == false);

                    if (bovedaActiva == null)
                    {
                        return Json(new { success = false, message = "Operación rechazada: La Bóveda de la oficina no está abierta o la sesión expiró." });
                    }

                    bovedaId = bovedaActiva.BovedaId; // Asignamos el ID real de la BD a tu variable original
                }
                // =======================================================================

                // 2. Validaciones de seguridad en el servidor (Tus validaciones intactas)
                if (tipoPagoOrigenId == tipoPagoDestinoId)
                {
                    return Json(new { success = false, message = "El banco de origen y el de destino no pueden ser iguales." });
                }
                if (importe <= 0)
                {
                    return Json(new { success = false, message = "El importe debe ser mayor a cero." });
                }

                // 3. Invocamos tu método de la capa de negocio (BL) (Tu lógica original intacta)
                bool exito = BovedaMovBL.TransferirEntreBancos(bovedaId, tipoPagoOrigenId, tipoPagoDestinoId, importe, glosa, usuarioRegId);

                if (exito)
                {
                    return Json(new { success = true, message = "¡Transferencia entre bancos realizada con éxito!" });
                }
                else
                {
                    return Json(new { success = false, message = "No se pudo registrar la transferencia en la base de datos." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error crítico en el servidor: " + ex.Message });
            }
        }
    }

}
