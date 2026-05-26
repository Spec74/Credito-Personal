using System.Collections.Generic;
using System.Web.Mvc;
using ITB.VENDIX.BL;
using System.Transactions;
using ITB.VENDIX.DA;
using System;

namespace Web.Controllers.Almacen
{
    public class SalidaController : Controller
    {
        // GET: Salida
        public ActionResult Index()
        {
            ViewBag.cboTipoMov = new SelectList(TipoMovimientoBL
                .Listar(x => x.Estado && x.IndEntrada == false && x.IndTransferencia == false && x.IndDevolucion == false && x.TipoMovimientoId != 2), "TipoMovimientoId", "Denominacion");
            return View();
        }
        public JsonResult BuscarProducto(string pNumeroSerie)
        {
            var ret = new RetornoSerie();
            var s = SerieArticuloBL.Obtener(x => x.NumeroSerie == pNumeroSerie);
            if (s == null)
            {
                ret.Error = true;
                ret.Mensaje = "El Producto no existe, ingrese otro";
            }
            else
            {
                if (s.EstadoId == 1)
                {
                    ret.Error = true;
                    ret.Mensaje = "El Producto se encuentra en estado SIN CONFIRMAR, ingrese otro.";
                }
                if (s.EstadoId == 2)
                {
                    var a = ArticuloBL.Obtener(s.ArticuloId);
                    ret.Error = false;
                    ret.SerieId = s.SerieArticuloId;
                    ret.Serie = s.NumeroSerie;
                    ret.ArticuloId = s.ArticuloId;
                    ret.Denominacion = a.Denominacion;
                }
                if (s.EstadoId == 3 || s.EstadoId == 4)
                {
                    ret.Error = true;
                    ret.Mensaje = "El Producto se encuentra en estado PREVENTA o VENDIDO, ingrese otro.";
                }
                if (s.EstadoId == 5)
                {
                    ret.Error = true;
                    ret.Mensaje = "El Producto se encuentra en estado ANULADO, ingrese otro.";
                }
            }

            return Json(ret, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult RealizarSalida(int tipoMovId, string glosa, List<SerieSalida> series)
        {
            var oficinaid = VendixGlobal.GetOficinaId();
            var almacenId = AlmacenBL.Obtener(x => x.OficinaId == oficinaid).AlmacenId;
            var listamovdet = new List<MovimientoDet>();


            using (var scope = new TransactionScope())
            {
                try
                {
                    var mov = new Movimiento()
                    {
                        TipoMovimientoId = tipoMovId,
                        AlmacenId = almacenId,
                        Fecha = VendixGlobal.GetFecha(),
                        SubTotal = 0,
                        IGV = 0,
                        AjusteRedondeo = 0,
                        TotalImporte = 0,
                        EstadoId = 3,
                        Observacion = glosa
                    };
                    MovimientoBL.Crear(mov);

                    foreach (var item in series)
                    {
                        bool f = false;
                        foreach (var i in listamovdet)
                        {
                            if (item.ArticuloId == i.ArticuloId)
                            {
                                f = true;
                                i.Cantidad++;
                                i.Descripcion += ", " + item.Serie;
                            }
                        }

                        if (f == false)
                        {
                            listamovdet.Add(new MovimientoDet()
                            {
                                MovimientoId = mov.MovimientoId,
                                Cantidad = 1,
                                ArticuloId = item.ArticuloId,
                                Descripcion = item.Denominacion + " " + item.Serie,
                                Descuento = 0,
                                Importe = 0,
                                IndCorrelativo = false,
                                UnidadMedidaT10 = 1
                            });
                        }
                    }

                    foreach (var item in listamovdet)
                    {
                        MovimientoDetBL.Crear(item);
                        var ser = series.FindAll(x => x.ArticuloId == item.ArticuloId);

                        SerieArticulo s;
                        foreach (var x in ser)
                        {
                            s = SerieArticuloBL.Obtener(x.SerieId);
                            s.EstadoId = 5; //anulado
                            s.MovimientoDetSalId = item.MovimientoDetId;
                            SerieArticuloBL.Actualizar(s);
                        }
                    }
                    scope.Complete();
                    return Json(string.Empty);
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return Json(ex.InnerException.Message);
                }
            }
        }
    }
    public class RetornoSerie
    {
        public bool Error { get; set; }
        public string Mensaje { get; set; }
        public int SerieId { get; set; }
        public int ArticuloId { get; set; }
        public string Serie { get; set; }
        public string Denominacion { get; set; }
    }

    public class SerieSalida
    {
        public int SerieId { get; set; }
        public string Serie { get; set; }
        public int ArticuloId { get; set; }
        public string Denominacion { get; set; }
    }

}