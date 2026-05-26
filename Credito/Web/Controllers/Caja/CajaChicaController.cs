using ITB.VENDIX.DA;
using ITB.VENDIX.BL;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Web.Controllers
{
    public class CajaChicaController : Controller
    {
        
        public ActionResult Index()
        {
            var usuarioId = VendixGlobal.GetUsuarioId();
            var cajadiario = CajaChicaDiarioBL.Obtener(x => x.UsuarioId == usuarioId && x.IndCierre == false);
            ViewBag.cboTipoOperacion = new SelectList(TipoOperacionBL.Listar(x => x.IndCajaChica), "TipoOperacionId", "Denominacion");
            ViewBag.cboTipoDocumento = new SelectList(TipoDocumentoBL.Listar(x => x.IndCajaChica && x.Estado), "TipoDocumentoId", "Denominacion");

            return View(cajadiario);
        }
        public ActionResult RealizarEntradaSalida(int pPersonaId, int pTipoOperacionId, string pDescripcion, decimal pImporte)
        {
            return Json(CajaChicaDiarioBL.EntradaSalida(pPersonaId, pTipoOperacionId, pDescripcion, pImporte)
                    , JsonRequestBehavior.AllowGet);
        }
        public ActionResult ObtenerCajaChicaDiario()
        {
            var usuarioId = VendixGlobal.GetUsuarioId();
            var cajadiario = CajaChicaDiarioBL.Obtener(x => x.UsuarioId == usuarioId && x.IndCierre == false);

            return Json(CajaChicaDiarioBL.Obtener(cajadiario.Id), JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarMovimientosJGrid(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = CajaChicaDiarioBL.LstMovimientosCajaChicaJGrid(request, ref totalRecords);
            var data = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstItem
                        select new
                        {
                            id = item.MovimientoCajaChicaId,
                            cell = new string[] {
                                                    item.MovimientoCajaChicaId.ToString(),
                                                    item.Estado?item.MovimientoCajaChicaId.ToString():"",
                                                    item.CajaChicaDiarioId.ToString(),
                                                    item.FechaReg.ToString(),
                                                    item.IndEntrada?"ENTRADA":"SALIDA",
                                                    item.Persona,
                                                    item.Operacion,
                                                    item.Descripcion,
                                                    item.ImportePago.ToString(),
                                                    item.Estado?"ACTIVO":"ANULADO"
                                                }
                        }
                       ).ToArray()
            };
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarRencionesPendientesJGrid(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = CajaChicaDiarioBL.LstRendiconesPendientesJGrid(request, ref totalRecords);

            var data = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstItem
                        select new
                        {
                            id = item.Id,
                            cell = new string[] {
                                                    item.Id.ToString(),
                                                    item.Persona.NombreCompleto,
                                                    item.Descripcion,
                                                    item.FechaReg.ToString(),
                                                    item.Importe.ToString(),
                                                    item.ImporteRendido.ToString()
                                                }
                        }
                       ).ToArray()
            };
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ListarRendicionGrd(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = CajaChicaDiarioBL.LstRendiconesJGrid(request, ref totalRecords);

            var data = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstItem
                        select new
                        {
                            id = item.Id,
                            cell = new string[] {
                                                    item.Id.ToString(),
                                                    item.RUC,
                                                    item.RazonSocial,
                                                    item.DetalleGasto,
                                                    item.TipoDocumento.Denominacion,
                                                    item.Fecha.ToShortDateString(),
                                                    item.Serie,
                                                    item.Numero,
                                                    item.Importe.ToString(),
                                                    item.Id.ToString(),
                                                }
                        }
                       ).ToArray()
            };
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult CrearMovimientoRendicion(int pMovimientoCajaChicaId, int pTipoDocumentoId, DateTime pFecha, 
                                               string pSerie, string pNumero, string pRUC, string pRazonSocial,
                                               string pDetalle,decimal pImporte)
        {            
            var movCajaChica = MovimientoCajaChicaBL.Obtener(x => x.Id == pMovimientoCajaChicaId, includeProperties: "MovimientoRendidoCajaChica");
            var sumRendido = movCajaChica.MovimientoRendidoCajaChica.Sum(x=>x.Importe);

            if ((sumRendido + pImporte)> movCajaChica.Importe)
            {
                return Json("La suma de las rendiciones debe ser menor o igual  " + movCajaChica.Importe, JsonRequestBehavior.AllowGet);
            }

            MovimientoRendidoCajaChicaBL.Guardar(new MovimientoRendidoCajaChica
            {
                MovimientoCajaChicaId = pMovimientoCajaChicaId,
                TipoDocumentoId= pTipoDocumentoId,
                Fecha= pFecha,
                Serie=pSerie,
                Numero= pNumero,
                RUC = pRUC,
                RazonSocial=pRazonSocial,
                DetalleGasto=pDetalle,
                Importe = pImporte
            });

            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }
        public ActionResult CerrarRendicion(int pMovimientoCajaChicaId)
        {
            var movCajaChica = MovimientoCajaChicaBL.Obtener(x => x.Id == pMovimientoCajaChicaId, includeProperties: "MovimientoRendidoCajaChica");
            var sumRendido = movCajaChica.MovimientoRendidoCajaChica.Sum(x => x.Importe);
            var devolucion = movCajaChica.Importe - sumRendido;
            movCajaChica.IndRendido = true;
            movCajaChica.ImporteRendido = sumRendido;
            MovimientoCajaChicaBL.Actualizar(movCajaChica);

            if (devolucion > 0)
            {
                CajaChicaDiarioBL.EntradaSalida(movCajaChica.PersonaId, Constante.TipoOperacion.DEVOLUCION, "DEVOLUCION DE GASTO", devolucion);
            }

            return Json(devolucion, JsonRequestBehavior.AllowGet);
        }
        public ActionResult TieneRencicionesPendientes()
        {
            var usuarioId = VendixGlobal.GetUsuarioId();
            var cajadiario = CajaChicaDiarioBL.Obtener(x => x.UsuarioId == usuarioId && x.IndCierre == false);
                       
            var pendientes = MovimientoCajaChicaBL.Contar(x => x.CajaChicaDiarioId==cajadiario.Id && x.Operacion == "GAS" && x.IndRendido == false);

            return Json(pendientes, JsonRequestBehavior.AllowGet);
        }
        public ActionResult CerrarCajaChicaDiario()
        {
            return Json(CajaChicaDiarioBL.CerrarCajaChicaDiario(), JsonRequestBehavior.AllowGet);
        }
        public ActionResult TransferirBoveda(decimal pMonto, string pDescripcion)
        {
            var boveda = BovedaBL.Obtener(VendixGlobal.GetBovedaId());

            if (boveda.IndCierre == false)
            {
                var oficinaId = VendixGlobal.GetOficinaId();
                var pUsuarRegId = VendixGlobal.GetUsuarioId();
                var oCajadiario = CajaChicaDiarioBL.Obtener(x => x.UsuarioId == pUsuarRegId && x.IndCierre == false);

                var rspta = CajaChicaDiarioBL.TransferirSaldosBoveda(pMonto, pDescripcion, oCajadiario.Id, boveda.BovedaId, oficinaId, pUsuarRegId);

                return Json(rspta, JsonRequestBehavior.AllowGet);
            }

            return Json(false, JsonRequestBehavior.AllowGet);
        }
        public ActionResult MostrarMontoCaja()
        {
            var ofid = VendixGlobal.GetOficinaId();
            var pUsuarRegId = VendixGlobal.GetUsuarioId();
            var oCajadiario = CajaChicaDiarioBL.Obtener(x => x.UsuarioId == pUsuarRegId && x.IndCierre == false);

            
            return Json(oCajadiario.SaldoFinal, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult EliminarComprobanteCajaChica(int pid) {
            MovimientoRendidoCajaChicaBL.Eliminar(pid);
            return Json(true, JsonRequestBehavior.AllowGet);
        }
    }
}