using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.BL;
using Web.Models;
using Helper;

namespace VendixWeb.Controllers.CajaDiario
{
    [Autenticado]
    public class CajaDiarioController : Controller
    {
        [HttpPost]
        public ActionResult MostrarMontoCaja()
        {
            var ofid = VendixGlobal.GetOficinaId();
            var cajaDiarioId = VendixGlobal.GetCajaDiarioId();

            //var monto = CajaDiarioBL.Obtener(x => x.CajaDiarioId == cajaDiarioId && x.IndCierre == false).SaldoFinal;
            var monto = CajaDiarioBL.ObtenerSaldoCuentaCajadiario(cajaDiarioId,1);
            return Json(monto.Value, JsonRequestBehavior.AllowGet);
        }

        public ActionResult TransferirSaldos(int? pCajaId, decimal pMonto, string pDescripcion)
        {
           
            var oficinaId = VendixGlobal.GetOficinaId();
            var pUsuarRegId = VendixGlobal.GetUsuarioId();
            var pCajaDiarioId = VendixGlobal.GetCajaDiarioId();

            if (pCajaId.HasValue)
            {
                var cajaDiarioDestino = CajaDiarioBL.Obtener(x=>x.CajaId==pCajaId && x.IndCierre==false);
                if (cajaDiarioDestino != null)
                {
                    var rspta = CajaDiarioBL.TransferirSaldosCajaDiario(pMonto, pDescripcion, pCajaDiarioId, cajaDiarioDestino.CajaDiarioId, oficinaId, pUsuarRegId);
                    return Json(rspta, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                var boveda = BovedaBL.Obtener(VendixGlobal.GetBovedaId());
                if (boveda.IndCierre == false)
                {
                    var rspta = CajaDiarioBL.TransferirSaldosBoveda(pMonto, pDescripcion, pCajaDiarioId, boveda.BovedaId, oficinaId, pUsuarRegId);
                    return Json(rspta, JsonRequestBehavior.AllowGet);
                }
            }
            

            return Json(false, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerMovimientoCajaAnular(int pMovimientoCajaId)
        {
            var rpta = new Respuesta() { Error = false };
            var movcaja = MovimientoCajaBL.Obtener(x=>x.MovimientoCajaId== pMovimientoCajaId,includeProperties:"CajaDiario,Persona");
            if (movcaja== null)
            {
                rpta.Error = true;
                rpta.Mensaje = "Movimiento no encontrado!";
                return Json(new { rpta = rpta }, JsonRequestBehavior.AllowGet);
            }
            if (movcaja.Estado==false)
            {
                rpta.Error = true;
                rpta.Mensaje = "El movimiento se encuentra Anulado!";
                return Json(new { rpta = rpta }, JsonRequestBehavior.AllowGet);
            }
            if (movcaja.CajaDiario.IndCierre == true)
            {
                rpta.Error = true;
                rpta.Mensaje = "Caja Diario Cerrado, no se puede anular!";
                return Json(new { rpta = rpta }, JsonRequestBehavior.AllowGet);
            }


            rpta.Valor = movcaja.FechaReg.ToString();
            rpta.Valor1 = movcaja.Persona.NombreCompleto; //UsuarioBL.ObtenerNombre(movcaja.CajaDiario.UsuarioAsignadoId);
            movcaja.CajaDiario = null;
            movcaja.Persona = null;
            return Json(new { rpta = rpta, val = movcaja }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ConfirmarClave(string clave)
        {
            var rpta = new Respuesta() { Error = false };
            var admin = UsuarioBL.Listar(x => x.NombreUsuario == "ADMVENDIX").FirstOrDefault();
            if (admin == null || !UsuarioPasswordHasherCompat.Verify(admin.ClaveUsuario, clave))
            {
                rpta.Error = true;
                rpta.Mensaje = "NO AUTORIZADO!!!!";
                return Json(rpta, JsonRequestBehavior.AllowGet);
            }
            
            return Json(rpta, JsonRequestBehavior.AllowGet);
        }
        public ActionResult EsIngresoTipoOperacion(int pTipoOperacionId)
        {
            var tipo = TipoOperacionBL.Obtener(pTipoOperacionId);
            return Json(tipo.IndEntrada, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ListarMovimientoCajaGrd(GridDataRequest request)
        {
            int totalRecords = 0; string totales = string.Empty;
            var lstItem = CajaDiarioBL.ListarMovimientoCajaGrd(request, ref totalRecords, ref totales);

            var data = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstItem
                        select new
                        {
                            id = item.MovimientoCajaId,
                            cell = new string[] {
                                                    item.MovimientoCajaId.ToString(),
                                                    item.Estado?item.MovimientoCajaId.ToString():"",
                                                    item.Operacion,
                                                    item.FechaReg.ToString(),
                                                    item.Descripcion,
                                                    item.ImportePago.ToString(),
                                                    item.Estado?"ACTIVO":"ANULADO",
                                                }
                        }
                       ).ToArray()
            };
            return Json(data, JsonRequestBehavior.AllowGet);
        }


    }

}

