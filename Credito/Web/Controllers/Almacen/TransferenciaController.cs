using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.DA;
using ITB.VENDIX.BL;

namespace VendixWeb.Controllers.Almacen
{
    public class TransferenciaController : Controller
    {

        public ActionResult Index()
        {
            var oficinaId = VendixGlobal.GetOficinaId();
            var lstalmacen = AlmacenBL.Listar(x => x.Estado && x.OficinaId == oficinaId);
            var lstTipoMov = TipoMovimientoBL.Listar(x => x.Estado && x.IndEntrada);

            ViewBag.cboAlmacen = new SelectList(AlmacenBL.Listar(x => x.Estado && x.OficinaId != oficinaId), "AlmacenId", "Denominacion");
            ViewBag.cboAlmacen2 = new SelectList(lstalmacen, "AlmacenId", "Denominacion");
            ViewBag.cboDestino = new SelectList(AlmacenBL.Listar(x => x.Estado && x.OficinaId != oficinaId), "AlmacenId", "Denominacion");
            ViewBag.cboTipoMovimiento2 = new SelectList(lstTipoMov, "TipoMovimientoId", "Denominacion");
            ViewBag.cboTipoDocumento = new SelectList(TipoDocumentoBL.Listar(x => x.Estado && x.IndAlmacen && x.IndAlmacenMov == false), "TipoDocumentoId", "Denominacion");
            ViewBag.cboMedida = new SelectList(ValorTablaBL.Listar(x => x.TablaId == 10 && x.ItemId > 0), "ItemId", "DesCorta");
            return View();
        }

        public ActionResult Listar(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = TransferenciaBL.LstTransferenciaJGrid(request, ref totalRecords);

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.TransferenciaId,
                            cell = new string[] {
                                                    item.TransferenciaId.ToString(),
                                                    item.AlmacenOrigen,
                                                    item.AlmacenDestino,
                                                    item.Fecha.ToString(),
                                                    item.Estado
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CrearTransferencia(int pAlmacenDestinoId)
        {
            var oficinaid = VendixGlobal.GetOficinaId();
            var usuarioid = VendixGlobal.GetUsuarioId();

            var item = new Transferencia
            {
                AlmacenOrigenId = AlmacenBL.Obtener(x => x.OficinaId == oficinaid).AlmacenId,
                AlmacenDestinoId = pAlmacenDestinoId,
                UsuarioId = usuarioid,
                Fecha = VendixGlobal.GetFecha(),
                Estado = "P"

            };
            TransferenciaBL.Crear(item);

            return Json(item.TransferenciaId, JsonRequestBehavior.AllowGet);

        }


        public ActionResult AgregarTransferenciaSerie(string pNumeroSerie, int pTransferenciaId)
        {
            string mensaje = string.Empty;
            var serie = SerieArticuloBL.Obtener(x => x.NumeroSerie == pNumeroSerie);

            TransferenciaSerieBL.Crear(new TransferenciaSerie
            {
                TransferenciaId = pTransferenciaId,
                SerieArticuloId = serie.SerieArticuloId
            });

            return Json(mensaje, JsonRequestBehavior.AllowGet);
        }


        public ActionResult ValidarSerie(string pNumeroSerie, int pTransferenciaId)
        {
            string mensaje = string.Empty;
            int pserie2 = int.Parse(pNumeroSerie);

            var serieA = SerieArticuloBL.Obtener(x => x.NumeroSerie == pNumeroSerie);

            // valida serie disponible
            if (serieA == null || serieA.EstadoId != 2)
            {
                mensaje = "La serie no esta disponible, ingrese otro.";
                return Json(mensaje, JsonRequestBehavior.AllowGet);
            }

            // valida series duplicadas
            var existe = TransferenciaSerieBL.Contar(x => x.TransferenciaId == pTransferenciaId && x.SerieArticuloId == serieA.SerieArticuloId);
            if (existe > 0)
            {
                mensaje = "La serie ya esta registrada, ingrese otro";
                return Json(mensaje, JsonRequestBehavior.AllowGet);
            }
            
            TransferenciaSerieBL.Crear(new TransferenciaSerie
            {
                TransferenciaId = pTransferenciaId,
                SerieArticuloId = serieA.SerieArticuloId
            });

            return Json(mensaje, JsonRequestBehavior.AllowGet);
        }



        //public ActionResult ListarDetalle(GridDataRequest request)
        //{
        //    int transferenciaid = request.DataFilters().Count > 0 ? int.Parse(request.DataFilters()["TransferenciaId"]) : 0;
        //    if (transferenciaid == 0)
        //        return Json(null, JsonRequestBehavior.AllowGet);

        //    const int totalRecords = 10;
        //    var productsData = new
        //    {
        //        total = 1,
        //        page = 1,
        //        records = totalRecords,
        //        rows = (from item in TransferenciaBL.ListarDetalleTransferencia(transferenciaid)
        //                select new
        //                {
        //                    id = item.ArticuloId,
        //                    cell = new string[] {
        //                                            item.TransferenciaId.ToString(),
        //                                            item.ArticuloId.ToString(),
        //                                            item.Articulo,
        //                                            item.Cantidad.ToString(),
        //                                            item.Series,
        //                                            item.TransferenciaId.ToString() + "," + item.ArticuloId.ToString()
        //                                        }
        //                }
        //               ).ToArray()
        //    };
        //    return Json(productsData, JsonRequestBehavior.AllowGet);
        //}

        public ActionResult ObtenerMovimiento(int pTransferenciaId)
        {
            return Json(TransferenciaBL.Obtener(pTransferenciaId), JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerMovimientoExt(int pTransferenciaId)
        {
            var tra = TransferenciaBL.ObtenerEntradaSalida(pTransferenciaId);
            return Json(new
            {
                Fecha = tra.Fecha.ToString(),
                tra.Estado,
                tra.AlmacenDestino,
                tra.AlmacenOrigen
            },
                JsonRequestBehavior.AllowGet);


        }

        public ActionResult EliminarTransferenciaSerie(int pTransferenciaId, int pArticuloId)
        {
            string qry = "DELETE ts " +
            "FROM ALMACEN.TransferenciaSerie ts " +
            "INNER JOIN ALMACEN.SerieArticulo sa ON ts.SerieArticuloId = sa.SerieArticuloId " +
            "WHERE ts.TransferenciaId = " + pTransferenciaId.ToString() + " and sa.ArticuloId = " + pArticuloId.ToString();
            TransferenciaSerieBL.EjecutarSql(qry);

            return Json(true, JsonRequestBehavior.AllowGet);
        }


        public ActionResult Desconfirmar(int pTransferenciaId)
        {
            return Json(TransferenciaBL.Desconfirmar(pTransferenciaId), JsonRequestBehavior.AllowGet);
        }

        
        [HttpPost]
        public ActionResult Confirmar(int pTransferenciaId)
        {
            return Json(TransferenciaBL.TransferirSeries(pTransferenciaId), JsonRequestBehavior.AllowGet);
        }

    }












}
