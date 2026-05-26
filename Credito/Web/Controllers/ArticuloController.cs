using System.Web;
using System.Web.Mvc;
using System.IO;
using ITB.VENDIX.BL;
using ITB.VENDIX.DA;
using Helper;

namespace VendixWeb.Controllers
{
    [Autenticado]
    public class ArticuloController : Controller
    {
        //
        // GET: /Articulo/
        public ActionResult Index()
        {
            ViewBag.cboModelo = new SelectList(ModeloBL.Listar(x => x.Estado), "ModeloId", "Denominacion");
            ViewBag.cboTipoArticulo = new SelectList(TipoArticuloBL.Listar(x => x.Estado), "TipoArticuloId", "Denominacion");
            return View();
        }

        [HttpPost]
        public ActionResult GuardarArticulo(int pArticuloId, int pModeloId, int pTipoArticuloId, string pCodArticulo, string pDenominacion,
                                            string pDescripcion, decimal pPrecio, decimal pDescuento, bool pIndPerecible, bool pIndImportado, bool pIndCanjeable, bool pActivo)
        {

            Articulo oarticulo;
            ListaPrecio oprecio;

            if (pArticuloId == 0)
            {
                oarticulo = new Articulo
                {
                    ArticuloId = pArticuloId,
                    ModeloId = pModeloId,
                    TipoArticuloId = pTipoArticuloId,
                    CodArticulo = pCodArticulo,
                    Denominacion = pDenominacion,
                    Descripcion = pDescripcion,
                    IndPerecible = pIndPerecible,
                    IndImportado = pIndImportado,
                    IndCanjeable = pIndCanjeable,
                    Estado = pActivo
                };
                ArticuloBL.Crear(oarticulo);

                oprecio = new ListaPrecio()
                {
                    ArticuloId = oarticulo.ArticuloId,
                    Monto = pPrecio,
                    Descuento = pDescuento,
                    Estado = pActivo
                };
                ListaPrecioBL.Crear(oprecio);
            }
            else
            {
                oarticulo = ArticuloBL.Obtener(pArticuloId);
                oarticulo.ModeloId = pModeloId;
                oarticulo.TipoArticuloId = pTipoArticuloId;
                oarticulo.CodArticulo = pCodArticulo;
                oarticulo.Denominacion = pDenominacion;
                oarticulo.Descripcion = pDescripcion;
                oarticulo.IndPerecible = pIndPerecible;
                oarticulo.IndImportado = pIndImportado;
                oarticulo.IndCanjeable = pIndCanjeable;
                oarticulo.Estado = pActivo;
                ArticuloBL.Actualizar(oarticulo);

                oprecio = ListaPrecioBL.Obtener(x => x.ArticuloId == pArticuloId);
                if (oprecio != null)
                {
                    oprecio.Monto = pPrecio;
                    oprecio.Descuento = pDescuento;
                    oprecio.Estado = pActivo;
                    ListaPrecioBL.Actualizar(oprecio);
                }
                else
                {
                    oprecio = new ListaPrecio()
                    {
                        ArticuloId = pArticuloId,
                        Monto = pPrecio,
                        Descuento = pDescuento,
                        Estado = pActivo
                    };
                    ListaPrecioBL.Crear(oprecio);
                }

            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GuardarImagen(HttpPostedFileBase archivo)
        {
            var pArticuloId = VendixGlobal<int>.Obtener("ArticuloId");
            
            string pFileName = archivo.FileName;
            archivo.SaveAs(Path.Combine(Server.MapPath("~/imgArticulos"), Path.GetFileName(pFileName)));

            var obj = ArticuloBL.Obtener(pArticuloId);
            var ruta = Path.Combine(Server.MapPath("~/imgArticulos"), Path.GetFileName(pFileName));
            string rutaimagencambiada = "";

            if (System.IO.File.Exists(ruta))
            {
                string nombreImagen;
                if (string.IsNullOrEmpty(obj.Imagen))
                    nombreImagen = pArticuloId + "_1";
                else
                    nombreImagen = pArticuloId + "_" + (int.Parse(obj.Imagen.Substring(obj.Imagen.Length - 5, 1)) + 1).ToString();
                // nombreImagen = pArticuloId + "_" + (obj.Imagen.Split(',').Length + 1).ToString();

                rutaimagencambiada = ruta.Replace(Path.GetFileName(ruta), nombreImagen + Path.GetExtension(ruta));

                if (System.IO.File.Exists(rutaimagencambiada))
                    System.IO.File.Delete(rutaimagencambiada);

                System.IO.File.Copy(ruta, rutaimagencambiada);
                System.IO.File.Delete(ruta);
            }

            if (string.IsNullOrEmpty(obj.Imagen))
                obj.Imagen = Path.GetFileName(rutaimagencambiada);
            else
                obj.Imagen = obj.Imagen + "," + Path.GetFileName(rutaimagencambiada);

            ArticuloBL.Actualizar(obj);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ObtenerImagen(int pArticuloId)
        {
            var obj = ArticuloBL.Obtener(pArticuloId);

            if (string.IsNullOrEmpty(obj.Imagen))
                return Json(null, JsonRequestBehavior.AllowGet);

            return Json(obj.Imagen.Split(','), JsonRequestBehavior.AllowGet);
        }

        public JsonResult BuscarArticuloSelect(string term)
        {
            return Json(ArticuloBL.BuscarArticuloSelect(term), JsonRequestBehavior.AllowGet);
        }
        public JsonResult BuscarArticuloAllSelect(string term)
        {
            return Json(ArticuloBL.BuscarArticuloAllSelect(term), JsonRequestBehavior.AllowGet);
        }

        public JsonResult BuscarListaArticulo(int pArticuloId)
        {
            var olista = new SelectList(ArticuloBL.Listar(x => x.ArticuloId == 15));
            return Json(olista, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ObtenerArticulo(int pArticuloId)
        {
            VendixGlobal<int>.Crear("ArticuloId", pArticuloId);
            return Json(new
            {
                Articulo = ArticuloBL.Obtener(pArticuloId),
                Precio = ListaPrecioBL.Obtener(x => x.ArticuloId == pArticuloId)
            }
                        , JsonRequestBehavior.AllowGet);
        }

        public ActionResult EliminarImagen(string pImagen, int pArticuloId)
        {
            var rutaimg = Path.Combine(Server.MapPath("~/imgArticulos"), pImagen);
            if (System.IO.File.Exists(rutaimg))
                System.IO.File.Delete(rutaimg);

            var obj = ArticuloBL.Obtener(pArticuloId);
            obj.Imagen = obj.Imagen.Split(',').Length == 1 ? string.Empty : obj.Imagen.Replace("," + pImagen, "").Replace(pImagen + ",", "");
            ArticuloBL.Actualizar(obj);

            return Json(true, JsonRequestBehavior.AllowGet);
        }

    }
}