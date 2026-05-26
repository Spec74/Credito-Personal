using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.DA;
using ITB.VENDIX.BL;

namespace VendixWeb.Controllers.Venta
{
    public class OrdenVentaController : Controller
    {

        public ActionResult Index(int id = 0,int pPersonaId = 0)
        {
            if (id == 0 && pPersonaId > 0)
            {
              var orden =  Repositorio<OrdenVenta>.Crear( new OrdenVenta()
                                        {
                                            OficinaId = VendixGlobal.GetOficinaId(),
                                            PersonaId = pPersonaId,
                                            Subtotal = 0,
                                            TotalDescuento = 0,
                                            TotalImpuesto = 0,
                                            TotalNeto = 0,
                                            TipoVenta = "CON",
                                            Estado = "PEN",
                                            UsuarioRegId = VendixGlobal.GetUsuarioId(),
                                            FechaReg = VendixGlobal.GetFecha()
                                        } );
                id = orden.OrdenVentaId;
            }

            if (id > 0)
            {
                return View(Repositorio<OrdenVenta>.Listar(x => x.OrdenVentaId == id, includeProperties: "OrdenVentaDet,Persona,Oficina").FirstOrDefault());
            }
            return View(new OrdenVenta { TotalNeto = 0, TotalDescuento = 0 });
        }
        public ActionResult OrdenesVenta()
        {
            return View();
        }

        public ActionResult BuscarOrdenVentaDet(string pOrdenVentaId)
        {
            var orden = Int32.Parse(pOrdenVentaId);
            var ordenventadet = OrdenVentaDetBL.Listar(x => x.OrdenVentaId == orden);
            return Json(new
            {
                OrdenVenta = Repositorio<OrdenVenta>.Obtener(orden),
                OrdenVentaDet = ordenventadet,
                Cantidadov = ordenventadet.Sum(x=>x.Cantidad)
            }

                        , JsonRequestBehavior.AllowGet);
        }

        public ActionResult AgregarOrdenVentaDetalle(string pNumeroSerie, string pOrdenVentaId)
        {
            var ordenVentaId = Int32.Parse(pOrdenVentaId);
            return Json(OrdenVentaDetBL.AgregarOrdenVentaDetalle(ordenVentaId, pNumeroSerie), JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerDetalleOrdenVenta(int pOrdenVentaDetId)
        {
            return Json(Repositorio<OrdenVentaDet>.Obtener(pOrdenVentaDetId), JsonRequestBehavior.AllowGet);
        }

        public ActionResult ActualizarDetalleOrdenVenta(int pOrdenVentaDetId, decimal pDescuento)
        {
            return Json(OrdenVentaDetBL.ActualizarOrdenVentaDetalle(pOrdenVentaDetId, pDescuento), JsonRequestBehavior.AllowGet);
        }

        public ActionResult EliminarDetalleOrdenVenta(int pOrdenVentaDetId)
        {
            return Json(OrdenVentaDetBL.EliminarOrdenVentaDetalle(pOrdenVentaDetId), JsonRequestBehavior.AllowGet);
        }
        public ActionResult EliminarOrdenVenta(int pOrdenVentaId)
        {
            return Json(OrdenVentaBL.EliminarOrdenVenta(pOrdenVentaId), JsonRequestBehavior.AllowGet);
        }

        public ActionResult ListarOrdenesVentaJgrid(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = OrdenVentaBL.LstOrdenesVentaJGrid(request, ref totalRecords);

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.OrdenVentaId,
                            cell = new string[] { 
                                                    item.OrdenVentaId.ToString(),
                                                    item.FechaReg.ToString(),
                                                    item.Cliente,
                                                    item.TotalDescuento.ToString(),
                                                    item.TotalNeto.ToString(),
                                                    ObtenerTipoOv(item.TipoVenta,item.EstadoCredito),
                                                    ObtenerCondicion(item.Estado,item.TipoVenta),
                                                    item.OrdenVentaId.ToString() + "," + ObtenerEliminarOv(item.Estado,item.TipoVenta,item.EstadoCredito)
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }
        private static string ObtenerEliminarOv(string pEstado, string pTipoVenta, string pEstadoCredito)
        {
            if (pEstado=="ENT") return "0";

            if (pEstado == "PEN") return "1";

            if (pTipoVenta=="CON") return "1";

            if (pTipoVenta=="CRE" && pEstadoCredito == "CRE") return "1";

            return "0";
        }

        private static string ObtenerCondicion(string pEstado, string pTipoVenta)
        {
            if (pEstado == "ANU")
                return "ANULADO";

            if (pEstado=="ENT")
                return "ENTREGADO";

            if (pEstado == "PEN")
                return "PENDIENTE";

            if (pEstado == "ENV" && pTipoVenta == "CON")
                return "POR COBRAR";
            if (pEstado == "ENV" && pTipoVenta == "CRE")
                return "EN EVALUACION";

            return String.Empty;
        }

        private static string ObtenerTipoOv(string pTipoVenta,string pEstadoCredito)
        {
            if (pTipoVenta=="CON") return "CONTADO";
            if (pTipoVenta == "CRE") return "CREDITO " + pEstadoCredito;
           
            return String.Empty;
        }

        public ActionResult EnviarOrdenVentaCredito(int pOrdenVentaId)
        {
            return Json(OrdenVentaBL.EnviarOrdenVentaCredito(pOrdenVentaId), JsonRequestBehavior.AllowGet);
        }
        public ActionResult EnviarOrdenVentaContado(int pOrdenVentaId)
        {
            return Json(OrdenVentaBL.EnviarOrdenVentaContado(pOrdenVentaId), JsonRequestBehavior.AllowGet);
        }
    }
}
