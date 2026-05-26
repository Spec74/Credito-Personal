using ITB.VENDIX.BL;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Web.Controllers.Venta
{
    public class VentaRapidaController : Controller
    {
        // GET: VentaRapida
        public ActionResult Index()
        {            
            var usuarioId = VendixGlobal.GetUsuarioId();
            var cajadiario = CajaDiarioBL.Obtener(x => x.UsuarioAsignadoId == usuarioId && x.IndCierre == false, includeProperties: "Caja");
            if (cajadiario != null) {
                VendixGlobal<int>.Crear("CajadiarioId", cajadiario.CajaDiarioId);
            }                

            return View(cajadiario);
        }
        public ActionResult ListarTipoDoc()
        {
            var dta = TipoDocumentoBL.Listar(x => x.Estado && x.IndVenta, x => x.OrderBy(y => y.Denominacion)).Select(x => new { Id = x.TipoDocumentoId, Valor = x.Denominacion });
            return Json(dta, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerArticulo(string pCodigo)
        {
            var art = ArticuloBL.Obtener(x => x.CodArticulo == pCodigo && x.Estado , "ListaPrecio");
            if (art == null || art.ListaPrecio.Count==0)
                return Json(null, JsonRequestBehavior.AllowGet);
            var stock = SerieArticuloBL.Contar(x => x.ArticuloId == art.ArticuloId && x.EstadoId == Constante.SerieArticulo.EN_ALMACEN);

            return Json(new
            {
                ArticuloId = art.ArticuloId,
                CodArticulo = art.CodArticulo,
                Denominacion = art.Denominacion,
                PrecioVenta = art.ListaPrecio.First().Monto,
                Stock = stock
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult RealizarPedido(int pClienteId, List<OrdenVentaBL.Pedido> pPedidos)
        {
            var ordenventaid = OrdenVentaBL.RealizarPedido(pClienteId, pPedidos);
            return Json(ordenventaid);
        }
    }
}
        
    