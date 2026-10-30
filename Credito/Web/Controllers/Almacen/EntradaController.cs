using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.DA;
using ITB.VENDIX.BL;


namespace VendixWeb.Controllers.Almacen
{
    public class EntradaController : Controller
    {
        const string CompraRetirada = "La entrada de almacén (compra a proveedores) no opera en Credix.";

        public ActionResult Index()
        {
            return View("Retirado");
        }

        public ActionResult Listar(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = MovimientoBL.LstEntradaJGrid(request, ref totalRecords);

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.MovimientoId,
                            cell = new string[] { 
                                                    item.MovimientoId.ToString(),
                                                    item.Tipo,
                                                    item.TipoMovimientoId.ToString(),
                                                    item.TipoMovimiento,
                                                    item.Fecha.ToString(),
                                                    item.Documento,
                                                    item.Estado,
                                                    item.Observacion
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Guardar(int pMovimientoId, int pAlmacenId, int pTipoMovimientoId, string pFecha, string pObservacion)
        {
            return Json(CompraRetirada, JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost]
        public ActionResult AgregarDocumento(int pMovimientoId, int pTipoDocumentoId, string pSerie, string pNumero)
        {
            return Json(CompraRetirada, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ListarDocs(GridDataRequest request)
        {
            int movimientoid = request.DataFilters().Count > 0 ? int.Parse(request.DataFilters()["MovimientoId"]) : 0;
            if (movimientoid==0)
                return Json(null, JsonRequestBehavior.AllowGet);

            const int totalRecords = 10;
            var productsData = new
            {
                total = 1,page = 1,records = totalRecords,
                rows = (from item in MovimientoDocBL.Listar(x => x.MovimientoId == movimientoid , null, "TipoDocumento,Movimiento")
                        select new
                        {
                            id = item.MovimientoDocId,
                            cell = new string[] { 
                                                    item.MovimientoDocId.ToString(),
                                                    item.TipoDocumento.Denominacion,
                                                    item.SerieDocumento,
                                                    item.NroDocumento,
                                                    item.Movimiento.EstadoId==1?item.MovimientoDocId.ToString():string.Empty
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Eliminar(int pid)
        {
            return Json(CompraRetirada);
        }

        public ActionResult ListarDetalle(GridDataRequest request)
        {
            int movimientoid = request.DataFilters().Count > 0 ? int.Parse(request.DataFilters()["MovimientoId"]) : 0;
            if (movimientoid == 0)
                return Json(null, JsonRequestBehavior.AllowGet);

            const int totalRecords = 10;
            var productsData = new
            {
                total = 1,
                page = 1,
                records = totalRecords,
                rows = (from item in MovimientoBL.ObtenerEntradaDetalle(movimientoid )
                        select new
                        {
                            id = item.MovimientoDetId,
                            cell = new string[] { 
                                                    item.MovimientoDetId.ToString(),
                                                    item.ArticuloId.ToString(),
                                                    item.UnidadMedidaT10.ToString(),
                                                    item.Cantidad.ToString(),
                                                    item.UnidadMedida,
                                                    item.Descripcion,
                                                    item.PrecioUnitario.ToString(),
                                                    item.Descuento.ToString(),
                                                    item.Importe.ToString(),
                                                    item.IndCorrelativo.ToString(),
                                                    item.Elimina?item.MovimientoDetId.ToString():string.Empty
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Delete(int pid)
        {
            return Json(CompraRetirada);
        }
        
        [HttpPost]
        public ActionResult CrearMovimientoDetalle(int pMovimientoId, int pMovimientoDetId, int pArticuloId, bool pIndAutogenerar, 
                                                    string pListaSerie, int pCantidad,bool pIndCorrelativo, decimal pPrecioUnitario, decimal pDescuento, int pMedida)
        {
            return Json(CompraRetirada);
        }
        [HttpPost]
        public ActionResult Confirmar(int pMovimientoId)
        {
            return Json(CompraRetirada, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ActualizarImporte(int pMovimientoId, decimal pAjuste)
        {
            return Json(CompraRetirada, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerMovimiento(int pMovimientoId)
        {
            return Json(MovimientoBL.Obtener(pMovimientoId), JsonRequestBehavior.AllowGet);
        }
        
        public ActionResult ObtenerMovimientoExt(int pMovimientoId)
        {
            var mov = MovimientoBL.ObtenerEntradaSalida(pMovimientoId);
            return Json(new {Fecha = mov.Fecha.ToString(), mov.Estado, mov.Almacen}, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerMovimientoDetalle(int pMovimientoDetId)
        {
            return
                Json(MovimientoDetBL.Listar(x => x.MovimientoDetId == pMovimientoDetId),
                     JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerSeriesMovimientoDetalle(int pMovimientoDetId)
        {
            return Json(SerieArticuloBL.Listar(x => x.MovimientoDetEntId == pMovimientoDetId).Select(c=> new{c.NumeroSerie}),
                                JsonRequestBehavior.AllowGet);
        }

        public ActionResult ValidarExisteSerie(string pListaSerie, bool pIndCorrelativo, int? pCantidad)
        {
            return Json(SerieArticuloBL.ValidarExisteSerie(pListaSerie, pIndCorrelativo, pCantidad), JsonRequestBehavior.AllowGet);
        }

        public ActionResult Desconfirmar(int pMovimientoId)
        {
            return Json(MovimientoBL.Desconfirmar(pMovimientoId), JsonRequestBehavior.AllowGet);
        }
    }
}
