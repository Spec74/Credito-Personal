using Helper;
using ITB.VENDIX.BL;
using ITB.VENDIX.DA;
using System.Linq;
using System.Web.Mvc;
using Web.Models;

namespace Web.Controllers.Credito
{
    [Autenticado]
    public class CondonacionController : Controller
    {
        // GET: Condonacion
        public ActionResult Index()
        {
            var condonaciones = CreditoCondonacionBL.Listar(
                x => !x.IndAprobado,
                q => q.OrderByDescending(y => y.Id),
                "Credito.Persona,Credito.Usuario")
                .Select(x => new CondonacionPendienteViewModel
                {
                    Id = x.Id,
                    CreditoId = x.CreditoId,
                    PersonaId = x.Credito != null ? x.Credito.PersonaId : 0,
                    NombreCliente = x.Credito != null && x.Credito.Persona != null
                        ? x.Credito.Persona.NombreCompleto
                        : string.Empty,
                    NombreUsuario = x.Credito != null && x.Credito.Usuario != null
                        ? x.Credito.Usuario.NombreUsuario
                        : string.Empty,
                    MontoCredito = x.Credito != null ? x.Credito.MontoCredito : 0m,
                    MoraCondonacion = x.MoraCondonacion,
                    TotalPago = x.TotalPago
                })
                .ToList();

            return View(condonaciones);
        }

        [HttpPost]
        public ActionResult Eliminar(int pid)
        {
            CreditoCondonacionBL.Eliminar(pid);
            return Json(true);
        }
    }
}
