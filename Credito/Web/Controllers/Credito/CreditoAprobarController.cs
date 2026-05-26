using Helper;
using ITB.VENDIX.BL;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Web.Controllers.Credito
{
    [Autenticado]
    public class CreditoAprobarController : Controller
    {
        // GET: CreditoAprobar
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ListarGrid(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = CreditoBL.LstCreditoAprobarJGrid(request, ref totalRecords);

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.CreditoId,
                            cell = new string[] {
                                                    item.CreditoId.ToString(),
                                                    item.PersonaId.ToString(),
                                                    item.Codigo,
                                                    item.Cliente,
                                                    item.Monto.ToString(),
                                                    item.Interes.ToString(),
                                                    item.Agente,
                                                    item.PersonaId.ToString()
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }
    }
}