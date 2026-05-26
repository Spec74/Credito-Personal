using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.BL;

namespace Web.Controllers.CajaDiario
{
    public class VerificarPagosController : Controller
    {
        // GET: VerificarPagos
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ListarGrid(GridDataRequest request)
        {            
            var lstGrd = CreditoBL.LstVerificarPagosJGrid();
            int totalRecords = lstGrd.Count;

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.MovimientoCajaId,
                            cell = new string[] {
                                                    item.MovimientoCajaId.ToString(),
                                                    item.Cliente,
                                                    item.Movimiento,                                                    
                                                    item.ImportePago.ToString(),
                                                    item.TipoPago,
                                                    item.FechaTransferencia,
                                                    item.Registro
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Verificar(int pMovimientoCajaId)
        {
            var item = MovimientoCajaExtensionBL.Obtener(pMovimientoCajaId);
            item.IndTransferenciaVerificada = true;
            MovimientoCajaExtensionBL.ActualizarParcial(item, x => x.IndTransferenciaVerificada);
                      
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }
        
    }
}