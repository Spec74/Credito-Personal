using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.BL;
using Helper;
using ITB.VENDIX.DA;

namespace VendixWeb.Controllers
{
    [Autenticado]
    public class BovedaController : Controller
    {
        //
        // GET: /Boveda/

        public ActionResult Index()
        {
            var oficinaid = VendixGlobal.GetOficinaId();

            ViewBag.cboCajas = new SelectList(CajaBL.ListarCajasAbiertas(), "id", "value");

            var oficinaId = VendixGlobal.GetOficinaId();
            ViewBag.cboOficinas = new SelectList(OficinaBL.Listar(x => x.Estado && x.OficinaId != oficinaId), "OficinaId", "Denominacion");
            ViewBag.cboTipoOperacion = new SelectList(TipoOperacionBL.Listar(x => x.IndBoveda, x => x.OrderBy(y => y.Denominacion)), "TipoOperacionId", "Denominacion");
            ViewBag.cboUsuario = new SelectList(UsuarioBL.Listar(x => x.Estado && x.NombreUsuario != "ADMVENDIX", x => x.OrderBy(y => y.NombreUsuario)), "UsuarioId", "NombreUsuario");
            ViewBag.cboTipoCuenta = new SelectList(ValorTablaBL.Listar(x => x.TablaId==13 && x.ItemId>0), "ItemId", "Denominacion");

            var oboveda = BovedaBL.Obtener(x => x.OficinaId == oficinaid && x.IndCierre == false);
            decimal dMontoCajaChica = 0, dMontoCajas = 0, dMontoPlanPagoPendiente = 0;
            //var oCajaChica = CajaChicaDiarioBL.Obtener(x => x.IndCierre == false && x.TransBoveda==false && x.Caja.OficinaId == oficinaId, includeProperties: "Caja");
            var oCajaChica = CajaChicaDiarioBL.Obtener(x => x.IndCierre == false && x.TransBoveda == false);
            if (oCajaChica != null)
                dMontoCajaChica = oCajaChica.SaldoFinal;

            var oCajas = CajaDiarioBL.Listar(x => x.IndCierre == false && x.TransBoveda == false && x.Caja.OficinaId == oficinaId, includeProperties: "Caja");
            if (oCajas != null)
                dMontoCajas = oCajas.Sum(x => x.SaldoFinal);

            dMontoPlanPagoPendiente = BovedaBL.MontoPendientePlanPago(oficinaId).Value;
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
            var listaTipoPago = ValorTablaBL.Listar(x => x.TablaId == 13 && x.ItemId > 0);
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
                                                    listaTipoPago.Where(x=>x.ItemId==item.TipoPagoId).ToList()[0].Denominacion,
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
            var bovedaInicioId = BovedaBL.Listar(x => x.OficinaId == oficinaId && x.IndCierre == false).FirstOrDefault().BovedaId;
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

    }

}
