using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.BL;
using Helper;
using System.Threading.Tasks;

namespace VendixWeb.Controllers.CajaDiario
{
    [Autenticado]
    public class SaldosController : Controller
    {
        //
        // GET: /Saldos/
        public ActionResult Index()
        {
            var usuarioId = VendixGlobal.GetUsuarioId();
            var oficinaId = VendixGlobal.GetOficinaId();
            var lectura = UsuarioRolBL.Contar(x => x.UsuarioId == usuarioId
                                                            && x.OficinaId == oficinaId
                                                            && x.Rol.Denominacion == "LECTURA_SALDO", includeProperties: "Rol");
            var rolAdmin = UsuarioRolBL.Contar(x => x.UsuarioId == usuarioId
                                                            && x.OficinaId == oficinaId
                                                            && x.Rol.Denominacion == "ADMINISTRADOR", includeProperties: "Rol");
            var permisoAnulacionMov = UsuarioRolBL.Contar(x => x.UsuarioId == usuarioId
                                                            && x.OficinaId == oficinaId
                                                            && x.Rol.Denominacion == "ANULACION_MOV", includeProperties: "Rol");
            ViewBag.EsLectura = false;
            if (lectura > 0) ViewBag.EsLectura = true;
            ViewBag.RolAdmin = false;
            if (rolAdmin > 0) ViewBag.RolAdmin = true;
            ViewBag.permisoAnulacionMov = false;
            if (permisoAnulacionMov > 0) ViewBag.permisoAnulacionMov = true;

            return View();
        }

        public ActionResult ListarSaldoCajaDiario(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = CajaDiarioBL.LstSaldosCajaDiarioJGrid(request, ref totalRecords);
            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstItem
                        select new
                        {
                            id = item.CajaDiarioId,
                            cell = new string[] { 
                                                    item.CajaDiarioId.ToString(),
                                                    item.Caja.Denominacion,
                                                    item.Usuario.NombreUsuario,
                                                    item.SaldoInicial.ToString(),
                                                    item.SaldoFinal.ToString(),
                                                    item.FechaIniOperacion.ToString(),
                                                    item.FechaFinOperacion.ToString(),
                                                    item.IndCierre ? "SI":"NO",
                                                    item.TransBoveda ? "SI":"NO",
                                                    item.CajaDiarioId.ToString()
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarSaldoCajaChicaDiario(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = CajaChicaDiarioBL.LstSaldosCajaChicaDiarioJGrid(request, ref totalRecords);
            var productsData = new
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
                                                    "CAJA CHICA",
                                                    item.Usuario.NombreUsuario,
                                                    item.SaldoInicial.ToString(),
                                                    item.SaldoFinal.ToString(),
                                                    item.FechaIniOperacion.ToString(),
                                                    item.FechaFinOperacion.ToString(),
                                                    item.IndCierre ? "SI":"NO",
                                                    item.TransBoveda ? "SI":"NO",
                                                    item.Id.ToString()
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarSaldoCajaDiarioBoveda(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = CajaDiarioBL.LstSaldosBovedaCajaDiarioJGrid(request, ref totalRecords);
            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstItem
                        select new
                        {
                            id = item.CajaDiarioId,
                            cell = new string[] {
                                                    item.CajaDiarioId.ToString(),
                                                    item.Caja,
                                                    item.Usuario,
                                                    item.SaldoInicial.ToString(),
                                                    item.SaldoFinal.ToString(),
                                                    item.FechaIniOperacion.ToString(),
                                                    item.FechaFinOperacion.ToString(),
                                                    item.IndCierre ? "SI":"NO",
                                                    item.TransBoveda ? "SI":"NO",
                                                    item.CajaDiarioId.ToString()
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarCajasAsignadas(GridDataRequest request)
        {
            var lstGrd = CajaBL.LstCajaDiarioOficina();

            var productsData = new
            {
                total = (int)Math.Ceiling((float)lstGrd.Count / (float)request.rows),
                page = request.page,
                records = lstGrd.Count,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.CajaDiarioId,
                            cell = new string[] { 
                                                    item.CajaDiarioId.ToString(),
                                                    item.Caja,
                                                    item.Modo,
                                                    item.Cajero,
                                                    item.FechaIniOperacion.ToString(),
                                                    item.FechaFinOperacion.HasValue?item.FechaFinOperacion.Value.ToString():string.Empty,
                                                    item.SaldoInicial.ToString(),
                                                    item.Entradas.ToString(),
                                                    item.Salidas.ToString(),
                                                    item.SaldoFinal.ToString()
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtnerListaCajasCombo()
        {
            var oficinaid = VendixGlobal.GetOficinaId();

            var olistaCajas = CajaBL.Listar(x => x.Estado  && x.IndAbierto == false && x.OficinaId== oficinaid)
                .Select(x => new { Id = x.CajaId, Valor = x.Denominacion });
            return Json(olistaCajas, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtnerListaUsuariosCombo()
        {
            var olistaUsuarios = CajaBL.ListaUsuariosNoAsignado();
            return Json(olistaUsuarios, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AsignarCaja(int pCajaId, int pUsuarioAsignadoId, decimal pSaldoInicial)
        {
            return Json(
                CajaDiarioBL.AsignarUsuarioCaja(pCajaId, pUsuarioAsignadoId, pSaldoInicial)
                , JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult Transferir(decimal pSobrante=0)
        {

            return Json(CajaDiarioBL.TransferirCajaDiarioBoveda(pSobrante), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> ActualizarDatosPostCierreBoveda()
        {
            await CajaDiarioBL.ActualizarDatosPostCierreBoveda();
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ValidarCierre()
        {
            var oficinaid = VendixGlobal.GetOficinaId();
            if (CajaDiarioBL.Contar(x => x.IndCierre == false && x.TransBoveda == false && x.Caja.OficinaId == oficinaid, "Caja") > 0)
                return Json("EXISTEN CAJAS ABIERTAS.", JsonRequestBehavior.AllowGet);

            if (CajaDiarioBL.Contar(x => x.IndCierre && x.TransBoveda == false && x.Caja.OficinaId == oficinaid, "Caja") == 0)
                return Json("NO EXISTEN CAJAS POR CERRAR.", JsonRequestBehavior.AllowGet);

            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult MostrarMontoBoveda()
        {
            var usuarioId = VendixGlobal.GetUsuarioId();
            var oficinaId = VendixGlobal.GetOficinaId();
            var encargado = UsuarioRolBL.Contar(x => x.UsuarioId == usuarioId
                                                            && x.OficinaId == oficinaId
                                                            && x.Rol.Denominacion == "ENCARGADO", includeProperties: "Rol");
            decimal monto = 0;
            if (encargado > 0)
                monto = BovedaBL.Obtener(x => x.OficinaId == oficinaId && x.IndCierre == false && x.IndTemporal == true).SaldoFinal;
            else
                monto = BovedaBL.Obtener(x => x.OficinaId == oficinaId && x.IndCierre == false && x.IndTemporal == false).SaldoFinal;

            return Json(monto, JsonRequestBehavior.AllowGet);
        }

        public ActionResult MostrarDetalleMovCaja(int pMovimientoCajaId)
        {
            var xxx = CajaDiarioBL.MostrarDetalleOvMovCaja(pMovimientoCajaId);
            return Json(xxx, JsonRequestBehavior.AllowGet);
        }


        public ActionResult ValidarCierreCajaChica()
        {
            var oficinaid = VendixGlobal.GetOficinaId();
            if (CajaChicaDiarioBL.Contar(x => x.IndCierre == false && x.TransBoveda == false) > 0)
                return Json("EXISTE CAJA ABIERTA.", JsonRequestBehavior.AllowGet);

            if (CajaChicaDiarioBL.Contar(x => x.IndCierre && x.TransBoveda == false ) == 0)
                return Json("NO EXISTEN CAJAS POR CERRAR.", JsonRequestBehavior.AllowGet);

            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult TransferirCierreCajaChica()
        {
            return Json(CajaChicaDiarioBL.TransferirCajaChicaDiarioBoveda(), JsonRequestBehavior.AllowGet);
        }
    }
}
