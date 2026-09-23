using Helper;
using ITB.VENDIX.DA;
using ITB.VENDIX.BL;
using ITB.VENDIX.BL.Creditos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using System.Web.Mvc;

namespace VendixWeb.Controllers
{
    [Autenticado]
    public class CreditoController : Controller
    {
        //
        // GET: /Credito/
        public ActionResult Creditos(int pPersonaId = 0)
        {
            if (pPersonaId > 0)
            {
                var usuarioId = VendixGlobal<int>.Obtener("UsuarioId");
                var oficinaId = VendixGlobal<int>.Obtener("OficinaId");
                var datos = new DatosCredito();

                datos.Persona = PersonaBL.Obtener(pPersonaId);
                datos.Cliente = ClienteBL.Obtener(x => x.PersonaId == pPersonaId);
                datos.SolicitudCredito = CreditoBL.Listar(x => x.Estado == "CRE" && x.PersonaId == pPersonaId && x.OficinaId == oficinaId,
                                    y => y.OrderByDescending(z => z.FechaReg), "Producto").FirstOrDefault();
                datos.Producto = ProductoBL.Listar(x => x.Estado).FirstOrDefault();
                datos.Creditos = CreditoBL.Listar(x => (x.Estado == "PEN" || x.Estado == "AP1" || x.Estado == "APR" || x.Estado == "DES")
                            && x.PersonaId == pPersonaId && x.OficinaId == oficinaId).ToList();
                datos.Avales = CreditoBL.ReporteAval(pPersonaId);

                datos.ClasificacionRiesgoSBS = (datos.Cliente.ClasificacionRiesgoSBS - 1).ToString();
                datos.ClasificacionRiesgoSBSObs = datos.Cliente.ClasificacionRiesgoSBSObs;

                datos.EstadoCliente = datos.Cliente.Estado ? "ACTIVO" : "INACTIVO";
                datos.TotalCreditos = CreditoBL.Contar(x => x.PersonaId == pPersonaId && x.Estado != "CRE" && x.OficinaId == oficinaId);
                if (datos.Persona.ConyuguePersonaId.HasValue)
                {
                    var conyugue = PersonaBL.Obtener(x => x.PersonaId == datos.Persona.ConyuguePersonaId.Value);
                    datos.Conyugue = conyugue.NombreCompleto;
                    datos.ConyugueDni = conyugue.NumeroDocumento;
                    datos.ConyugueCelular = conyugue.Celular1;
                }

                if (datos.Persona.EstadoCivilId.HasValue)
                    datos.EstadoCivil = ValorTablaBL.Obtener(x => x.TablaId == 11 && x.ItemId == datos.Persona.EstadoCivilId).Denominacion;
                if (datos.Persona.TipoViviendaId.HasValue)
                    datos.TipoVivienda = ValorTablaBL.Obtener(x => x.TablaId == 12 && x.ItemId == datos.Persona.TipoViviendaId).Denominacion;
                if (datos.Cliente.ActividadEconId.HasValue)
                    datos.ActividadEconomica = OcupacionBL.Obtener(x => x.OcupacionId == datos.Cliente.ActividadEconId).Denominacion;

                switch (datos.Cliente.Calificacion)
                {
                    case "A": datos.CalificacionCliente = "BUENO"; break;
                    case "B": datos.CalificacionCliente = "REGULAR"; break;
                    case "C": datos.CalificacionCliente = "MALO"; break;
                    default: datos.CalificacionCliente = "NO TIENE"; break;
                }

                ViewBag.PersonaId = pPersonaId;
                ViewBag.Cliente = datos.Persona.NombreCompleto;
                ViewBag.cboProducto = new SelectList(ProductoBL.Listar(x => x.Estado), "ProductoId", "Denominacion");
                ViewBag.cboAnalista = new SelectList(UsuarioBL.Listar(x => x.Estado, includeProperties: "Persona"), "UsuarioId", "Persona.NombreCompleto");
                ViewBag.Aprobador1 = UsuarioRolBL.Contar(x => x.UsuarioId == usuarioId
                                                            && x.OficinaId == oficinaId
                                                            && x.Rol.Denominacion == "APROBADOR 1", includeProperties: "Rol");
                ViewBag.Administrador = UsuarioRolBL.Contar(x => x.UsuarioId == usuarioId
                                                            && x.OficinaId == oficinaId
                                                            && x.Rol.Denominacion == "ADMINISTRADOR", includeProperties: "Rol");
                var lectura = UsuarioRolBL.Contar(x => x.UsuarioId == usuarioId
                                                            && x.OficinaId == oficinaId
                                                            && x.Rol.Denominacion == "LECTURA", includeProperties: "Rol");
                if (lectura > 0)
                    datos.lectura = true;

                if (datos.SolicitudCredito != null)
                    VendixGlobal<int>.Crear("SolicitudCreditoId", datos.SolicitudCredito.CreditoId);

                var depurado = PersonaDepuradoBL.Obtener(x => x.PersonaId == pPersonaId && x.Estado);
                if (depurado != null)
                    ViewBag.Depurado = depurado.Descripcion;

                return View(datos);
            }
            return View();
        }
        public class PagoPlanillaDTO
        {
            public int CreditoId { get; set; }
            public decimal MontoPagar { get; set; }
            public int TipoPagoId { get; set; }
            public string FechaHoraTrans { get; set; }
        }

        public ActionResult ParametrosSimulador()
        {
            ViewBag.FactorVariable = ValorTablaBL.Obtener(x => x.TablaId == 3 && x.ItemId == 1).Valor;
            ViewBag.FactorFijo = ValorTablaBL.Obtener(x => x.TablaId == 3 && x.ItemId == 2).Valor;

            return View();
        }

        [HttpPost]
        public ActionResult ActualizarParametrosSimulador(string pFactorVariable, string pFactorFijo)
        {
            var fvar = ValorTablaBL.Obtener(x => x.TablaId == 3 && x.ItemId == 1);
            fvar.Valor = pFactorVariable;
            ValorTablaBL.Actualizar(fvar);

            var ffijo = ValorTablaBL.Obtener(x => x.TablaId == 3 && x.ItemId == 2);
            ffijo.Valor = pFactorFijo;
            ValorTablaBL.Actualizar(ffijo);

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Simulador(int pProductoId = 1, string pTipo = "V", decimal pMonto = 0, int pCuotas = 0, decimal pInteres = 0,
     string pFecha = "", string pModalidad = "", decimal? pGastosAdm = null, string pGA = "CAP")
        {
            if (!string.IsNullOrEmpty(Request["cboProductos"]))
            {
                int.TryParse(Request["cboProductos"], out pProductoId);
            }
            if (!string.IsNullOrEmpty(Request["cboModalidad"]))
            {
                pModalidad = Request["cboModalidad"];
            }

            if (string.IsNullOrEmpty(pModalidad))
            {
                pModalidad = "M";
            }
            ViewBag.pMonto = pMonto;
            ViewBag.pCuotas = pCuotas;
            ViewBag.pInteres = pInteres;
            ViewBag.pProductoId = pProductoId;

            var producto = ProductoBL.Obtener(pProductoId);
            ViewBag.pProducto = producto != null ? producto.Denominacion : "PRODUCTO";

            ViewBag.pFecha = pFecha;
            ViewBag.pModalidadVal = pModalidad;

            var periodoAnio = 12.0;
            switch (pModalidad)
            {
                case "D": ViewBag.pModalidad = "DIARIO"; periodoAnio = 360.0; break;
                case "S": ViewBag.pModalidad = "SEMANAL"; periodoAnio = 52.0; break;
                case "Q": ViewBag.pModalidad = "QUINCENAL"; periodoAnio = 24.0; break;
                case "M": ViewBag.pModalidad = "MENSUAL"; periodoAnio = 12.0; break;
                default: ViewBag.pModalidad = "MENSUAL"; periodoAnio = 12.0; break;
            }

            var pTem = pMonto > 0 ? Math.Pow(double.Parse((1 + pInteres / 100).ToString()), 1 / periodoAnio) - 1 : 0;
            ViewBag.TEM = Math.Round(pTem, 6);

            string tipoPersona = Request["sim_p.TipoPersona"] ?? "N";
            string nroDocumento = Request["sim_p.NumeroDocumento"] ?? "";
            string nombre = Request["sim_p.Nombre"] ?? "";
            string apePaterno = Request["sim_p.ApePaterno"] ?? "";
            string apeMaterno = Request["sim_p.ApeMaterno"] ?? "";

            ViewBag.pGastosAdm = pGastosAdm ?? 0;
            ViewBag.pGA = pGA;
            ViewBag.tipoPersona = tipoPersona;
            ViewBag.nroDocumento = nroDocumento;

            ViewBag.nombreCompleto = tipoPersona == "N" ? $"{nombre} {apePaterno} {apeMaterno}".Trim() : nombre;
            if (string.IsNullOrEmpty(ViewBag.nombreCompleto)) ViewBag.nombreCompleto = "CLIENTE PROSPECTO";

            ViewBag.direccionCliente = Request["sim_p.DireccionCliente"] ?? "";
            ViewBag.direccionNegocio = Request["sim_p.DireccionNegocio"] ?? "";
            ViewBag.prendaDescripcion = Request["sim_PrendaDescripcion"] ?? "";
            string nombreGlobal = ITB.VENDIX.BL.VendixGlobal<string>.Obtener("NombreCompletoAsesor");

            ViewBag.pAsesor = !string.IsNullOrWhiteSpace(nombreGlobal) ? nombreGlobal : User.Identity.Name;
            ViewBag.pTelefono = Request["sim_p.Telefono"] ?? "";
            ViewBag.pFechaDesembolso = Request["pFechaDesembolso"] ?? pFecha;
            ViewBag.pFechaVencimiento = Request["pFechaVencimiento"] ?? "";
            ViewBag.pObservaciones = Request["pObservaciones"] ?? "El atraso en el pago genera intereses moratorios según reglamento interno de Crediconfiable.";
            DateTime fechaPrimerPago;
            if (!DateTime.TryParse(pFecha, out fechaPrimerPago))
            {
                fechaPrimerPago = DateTime.Now;
            }

            List<usp_SimuladorCredito_Result> oPlanPago = pMonto > 0
                    ? CreditoBL.SimuladorCredito(pMonto, pModalidad, pCuotas, pInteres, fechaPrimerPago, pGA == "CUO" ? (pGastosAdm ?? 0) : 0)
                    : new List<usp_SimuladorCredito_Result>();

            return View(oPlanPago);
        }


        public ActionResult CrearSolicitudCredito(int pPersonaId)
        {
            return Json(CreditoBL.CrearSolicitudCredito(pPersonaId), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GenerarCredito(int pProductoId, string pTipoCuota, decimal pMontoInicial, decimal pMontoGastosAdm, string pIndGastosAdm,
                            decimal pMontoCredito, string pModalidad, int pNumerocuotas, decimal pInteresMensual, string pFecha, string pObservacion,
                            bool pIndCentralRiesgo)
        {

            var rpta = CreditoBL.CrearCredito(VendixGlobal<int>.Obtener("SolicitudCreditoId"), pProductoId, pTipoCuota, pMontoInicial, pMontoGastosAdm, pIndGastosAdm,
                pMontoCredito, pModalidad, pNumerocuotas, pInteresMensual, DateTime.Parse(pFecha), pObservacion,
                pIndCentralRiesgo);
            return Json(rpta, JsonRequestBehavior.AllowGet);
        }
        public ActionResult AprobarCredito1ra(int pCreditoId)
        {
            var rpta = CreditoBL.AprobarCredito(pCreditoId, 0);
            return Json(rpta, JsonRequestBehavior.AllowGet);
        }
        public ActionResult AprobarCredito(int pCreditoId)
        {
            var rpta = CreditoBL.AprobarCredito(pCreditoId, 1);
            return Json(rpta, JsonRequestBehavior.AllowGet);
        }
        public ActionResult RechazarCredito(int pCreditoId)
        {
            return Json(CreditoBL.RechazarCredito(pCreditoId), JsonRequestBehavior.AllowGet);
        }
        public ActionResult ReprogramarCredito(int pCreditoId)
        {
            return Json(CreditoBL.ReprogramarCredito(pCreditoId), JsonRequestBehavior.AllowGet);
        }
        public ActionResult GuardarCargo(int pCreditoId, int pTipoCargoId, decimal pMonto, string pDescripcion, bool pFinal)
        {
            var rpta = CargoBL.CrearCargo(pCreditoId, pTipoCargoId, pMonto, pDescripcion, pFinal);
            return Json(rpta, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ValidarAnularCredito(int pCreditoId)
        {
            if (PlanPagoBL.Contar(x => x.Estado == "PAG" && x.CreditoId == pCreditoId) > 0)
                return Json(false, JsonRequestBehavior.AllowGet);

            var cant = CuentaxCobrarBL.Contar(x => x.CreditoId == pCreditoId);
            var cant1 = CuentaxCobrarBL.Contar(x => x.CreditoId == pCreditoId && (x.Estado == "ANU" || x.Estado == "PEN"));

            if (cant == cant1)
                return Json(true, JsonRequestBehavior.AllowGet);

            return Json(false, JsonRequestBehavior.AllowGet);
        }
        public ActionResult AnularCredito(int pCreditoId, string pObservacion)
        {
            return Json(CreditoBL.AnularCredito(pCreditoId, pObservacion.ToUpper()), JsonRequestBehavior.AllowGet);
        }
        public ActionResult DepurarCredito(int pPersonaId, string pObservacion)
        {
            return Json(CreditoBL.DepurarCredito(pPersonaId, pObservacion.ToUpper()), JsonRequestBehavior.AllowGet);
        }
        public ActionResult ActualizarAvalCredito(int pCreditoId, int? pPersonaId)
        {
            return Json(CreditoBL.ActualizarAvalCredito(pCreditoId, pPersonaId), JsonRequestBehavior.AllowGet);
        }
        public ActionResult ObtenerCredito(int pCreditoId)
        {
            return Json(CreditoBL.ObtenerDatoCredito(pCreditoId), JsonRequestBehavior.AllowGet);
        }
        public ActionResult ObtenerProducto(int pProductoId)
        {
            var rpta = ProductoBL.Obtener(pProductoId);
            return Json(rpta, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarProductos()
        {
            var prod = ProductoBL.Listar(x => x.Estado).Select(x => new { Id = x.ProductoId, Valor = x.Denominacion });
            return Json(prod, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarTipoCargo()
        {
            var dta = ValorTablaBL.Listar(x => x.TablaId == 2 && x.ItemId > 0).Select(x => new { Id = x.ItemId, Valor = x.Denominacion });
            return Json(dta, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarCargoGrd(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = CargoBL.ListarCargoJGrid(request, ref totalRecords);

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.CargoId,
                            cell = new string[] {
                            item.CargoId.ToString(),
                            item.UsuarioCargo,
                            item.TipoCargo,
                            item.Importe.ToString(),
                            item.Descripcion,
                            item.NumCuota.ToString(),
                            item.Estado
                        }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarCreditoPendienteGrd(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = CajaDiarioBL.LstCreditoPendienteJGrid(request, ref totalRecords);

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
                            item.Cliente,

                            (item.MontoCredito + item.Interes).ToString("N2"),

                            item.PersonaId.ToString(),
                            item.FechaVencimiento.HasValue ? item.FechaVencimiento.Value.ToString("yyyy-MM-dd") : "",
                            item.ImporteMora.ToString("N2"),

                            item.DeudaPendiente.ToString("N2")
                        }
                        }).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarCreditoPendienteBloqueGrd(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = CajaDiarioBL.LstCreditoPendienteJGrid(request, ref totalRecords);

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
                                item.Cliente,

                                "", // Nueva columna: botón Ver Perfil

                                (item.MontoCredito + item.Interes).ToString("N2"),

                                item.PersonaId.ToString(),

                                item.FechaVencimiento.HasValue
                                    ? item.FechaVencimiento.Value.ToString("yyyy-MM-dd")
                                    : "",

                                item.ImporteMora.ToString("N2"),

                                item.DeudaPendiente.ToString("N2")
                            }
                        }).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CobrarPlanillaBloque(List<PagoPlanillaDTO> planilla)
        {
            if (planilla == null || !planilla.Any())
                return Json(new { Exito = false, Mensaje = "La planilla enviada está vacía." });

            int cajaId = VendixGlobal.GetCajaDiarioId();

            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                try
                {
                    foreach (var item in planilla.Where(x => x.MontoPagar > 0))
                    {
                        string[] formatosPermitidos = new string[] {
                    "dd/MM/yyyy HH:mm",
                    "dd/MM/yyyy HH:mm:ss",
                    "yyyy-MM-ddTHH:mm",
                    "yyyy-MM-ddTHH:mm:ss",
                    "yyyy-MM-dd HH:mm:ss"
                };

                        if (!DateTime.TryParseExact(item.FechaHoraTrans, formatosPermitidos,
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out DateTime fechaParsed))
                        {
                            if (!DateTime.TryParse(item.FechaHoraTrans, out fechaParsed))
                            {
                                fechaParsed = DateTime.Now;
                            }
                        }

                        string fechaSeguraParaBL = fechaParsed.ToString("dd/MM/yyyy HH:mm");

                        var res = CajaDiarioBL.PagarCuotaPagoLibre(cajaId, item.CreditoId, item.MontoPagar, item.TipoPagoId, fechaSeguraParaBL);

                        if (res == -1 || res == null)
                            throw new Exception($"Error al procesar el pago del Crédito ID {item.CreditoId}.");
                    }

                    bool impagosOk = CreditoBL.CompletarImpagos();
                    if (!impagosOk)
                        throw new Exception("Ocurrió un error en la base de datos al intentar completar los impagos.");

                    scope.Complete();
                    return Json(new { Exito = true, Mensaje = "Planilla e impagos procesados con éxito." });
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    string error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return Json(new { Exito = false, Mensaje = "Proceso abortado por seguridad: " + error });
                }
            }
        }
        public ActionResult ListarCuotasPorCredito(int pCreditoId)
        {
            try
            {
                if (pCreditoId <= 0)
                    return Json(new { error = "ID de crédito inválido." }, JsonRequestBehavior.AllowGet);

                var lstPlanPago = CreditoBL.ListarEstadoPlanPago(pCreditoId);

                if (lstPlanPago == null || !lstPlanPago.Any())
                    return Json(new { rows = new object[0] }, JsonRequestBehavior.AllowGet);

                var todasLasCuotas = lstPlanPago
                    .OrderBy(x => x.FechaVencimiento)
                    .Select(item => new
                    {
                        id = item.PlanPagoId,
                        NroCuota = item.Numero != null ? item.Numero.ToString() : "N/A",
                        FechaVenc = item.FechaVencimiento != null ? Convert.ToDateTime(item.FechaVencimiento).ToString("dd/MM/yyyy") : "",
                        Monto = (item.PagoCuota ?? 0).ToString("F2"),
                        Estado = item.Estado
                    }).ToList();

                return Json(new { rows = todasLasCuotas.ToArray() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult CalcularGastosAdm(int pProductoId, decimal pMonto, bool pIncluyeCentralRiesgo)
        {
            return Json(GastosAdmBL.CalcularGastosAdm(pMonto, pIncluyeCentralRiesgo), JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerFechaPrimerPago(string pModalidad)
        {
            var fecha = VendixGlobal.GetFecha();
            switch (pModalidad)
            {
                case "D": fecha = fecha.AddDays(1); break;
                case "S": fecha = fecha.AddDays(7); break;
                case "Q": fecha = fecha.AddDays(15); break;
                case "M": fecha = fecha.AddMonths(1); break;
            }
            return Json(fecha.ToString("dd/MM/yyyy"), JsonRequestBehavior.AllowGet);
        }


        public ActionResult ListarCreditosGrd(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = CreditoBL.ListarCreditosGrd(request, ref totalRecords);

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
                                                    item.FechaDesembolso.HasValue? item.FechaDesembolso.Value.ToShortDateString():"",
                                                    item.MontoCredito.ToString(),
                                                    item.Interes.ToString(),
                                                    item.FormaPago,
                                                    item.NumeroCuotas.ToString(),
                                                    item.Calificacion,
                                                    item.FechaPagado.HasValue?item.FechaPagado.Value.ToShortDateString():"",
                                                    item.Estado=="PAG"?"CANCELADO":item.Estado
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarPlanPagoHistGrd(GridDataRequest request, int pCreditoId)
        {
            var lstGrd = PlanPagoBL.Listar(x => x.CreditoId == pCreditoId);
            int totalRecords = lstGrd.Count;
            lstGrd.Add(new PlanPago()
            {
                PlanPagoId = 0,
                Amortizacion = lstGrd.Sum(x => x.Amortizacion),
                Interes = lstGrd.Sum(x => x.Interes),
                GastosAdm = lstGrd.Sum(x => x.GastosAdm),
                ImporteMora = lstGrd.Sum(x => x.ImporteMora)
            });

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.PlanPagoId,
                            cell = new string[] {
                                                   item.PlanPagoId==0?String.Empty:item.Numero.ToString(),
                                                    item.PlanPagoId==0?String.Empty:item.Capital.ToString(),
                                                    item.PlanPagoId==0?"TOTAL:":item.FechaVencimiento.ToShortDateString(),
                                                    item.Amortizacion.ToString(),
                                                    item.Interes.ToString(),
                                                    item.GastosAdm.ToString(),
                                                    item.PlanPagoId==0?String.Empty:item.Cuota.ToString(),
                                                    item.ImporteMora.ToString(),
                                                    item.Descuento.ToString(),
                                                    item.PlanPagoId==0?String.Empty:item.PagoCuota.ToString(),
                                                    item.Estado,
                                                    item.FechaPagoCuota.HasValue?item.FechaPagoCuota.Value.ToShortDateString():string.Empty
                                                }
                        }
                       ).ToArray()
            };

            return Json(productsData, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ObtenerCreditoPendiente(int pCreditoId)
        {
            var lstGrd = CreditoBL.ListarEstadoPlanPago(pCreditoId);
            var lstGrdPen = lstGrd.Where(x => x.Estado == "PEN").ToList();

            var Creditototal = PlanPagoBL.Listar(x => x.CreditoId == pCreditoId).Sum(x => x.Cuota);
            var Pagoscuota = MovimientoCajaBL.Listar(x => x.CreditoId == pCreditoId && x.Estado && x.Operacion == "CUO").Sum(x => x.ImportePago);


            var pendiente = new usp_EstadoPlanPago_Result()
            {
                Cuota = Creditototal - Pagoscuota,
                Amortizacion = lstGrdPen.Sum(x => x.Amortizacion),
                Interes = lstGrdPen.Sum(x => x.Interes) + lstGrdPen.Sum(x => x.GastosAdm),
                ImporteMora = lstGrdPen.Sum(x => x.ImporteMora),
                PagoLibre = lstGrdPen.Sum(x => x.PagoLibre),
                Cargo = lstGrdPen.Sum(x => x.Cargo)
            };

            var condonacionPendiente = CreditoCondonacionBL.Obtener(x => x.CreditoId == pCreditoId && !x.IndAprobado);

            return Json(new
            {
                pendiente.Cuota,
                pendiente.Amortizacion,
                pendiente.Interes,
                pendiente.ImporteMora,
                pendiente.PagoLibre,
                pendiente.Cargo,
                TieneMoraCondonacionPendiente = condonacionPendiente != null,
                MoraCondonacionPendiente = condonacionPendiente != null ? condonacionPendiente.MoraCondonacion : 0m
            }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarPlanPagoActGrd(GridDataRequest request)
        {
            var lstGrd = CreditoBL.ListarEstadoPlanPago(int.Parse(request.DataFilters()["pCreditoId"]));
            int totalRecords = lstGrd.Count;
            decimal? pagoLibre = lstGrd.Where(x => x.Estado == "PEN").Sum(x => x.PagoLibre);
            if (!pagoLibre.HasValue) pagoLibre = 0;

            var lstGrdPen = lstGrd.Where(x => x.Estado == "PAG").ToList();
            lstGrd.Add(new usp_EstadoPlanPago_Result()
            {
                PlanPagoId = -1,
                Amortizacion = lstGrdPen.Sum(x => x.Amortizacion),
                Interes = lstGrdPen.Sum(x => x.Interes),
                GastosAdm = lstGrdPen.Sum(x => x.GastosAdm),
                Cuota = lstGrdPen.Sum(x => x.Cuota) + pagoLibre.Value,
                ImporteMora = lstGrdPen.Sum(x => x.ImporteMora),
                //PagoLibre = lstGrdPen.Sum(x => x.PagoLibre),
                PagoCuota = lstGrdPen.Sum(x => x.PagoCuota) + pagoLibre.Value
            });
            lstGrdPen = lstGrd.Where(x => x.Estado == "PEN").ToList();
            lstGrd.Add(new usp_EstadoPlanPago_Result()
            {
                PlanPagoId = -2,
                Amortizacion = lstGrdPen.Sum(x => x.Amortizacion),
                Interes = lstGrdPen.Sum(x => x.Interes),
                GastosAdm = lstGrdPen.Sum(x => x.GastosAdm),
                Cuota = lstGrdPen.Sum(x => x.Cuota) - pagoLibre.Value,
                ImporteMora = lstGrdPen.Sum(x => x.ImporteMora),
                //PagoLibre = lstGrdPen.Sum(x => x.PagoLibre),
                PagoCuota = lstGrdPen.Sum(x => x.PagoCuota)
            });
            lstGrdPen = lstGrd.Where(x => x.Estado == "PEN" || x.Estado == "PAG" || x.Estado == "CRE").ToList();
            lstGrd.Add(new usp_EstadoPlanPago_Result()
            {
                PlanPagoId = 0,
                Amortizacion = lstGrdPen.Sum(x => x.Amortizacion),
                Interes = lstGrdPen.Sum(x => x.Interes),
                GastosAdm = lstGrdPen.Sum(x => x.GastosAdm),
                Cuota = lstGrdPen.Sum(x => x.Cuota),
                ImporteMora = lstGrdPen.Sum(x => x.ImporteMora),
                //PagoLibre = lstGrdPen.Sum(x => x.PagoLibre),
                PagoCuota = lstGrdPen.Sum(x => x.PagoCuota) + pagoLibre.Value
            });

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.PlanPagoId,
                            cell = new string[] {
                                                   item.PlanPagoId<=0?String.Empty:item.Numero.ToString(),
                                                    item.PlanPagoId<=0?String.Empty:item.Capital.ToString(),
                                                    item.PlanPagoId>0?item.FechaVencimiento.ToShortDateString():(item.PlanPagoId==0?"TOTAL":(item.PlanPagoId==-1?"PAGADO":"PENDIENTE")),
                                                    item.Amortizacion.ToString(),
                                                    item.Interes.ToString(),
                                                    item.GastosAdm.ToString(),
                                                    item.Cuota.ToString(),
                                                    item.DiasAtrazo.ToString(),
                                                    item.ImporteMora.ToString(),
                                                    item.Descuento.ToString() +";"+ item.Estado + ";" + item.PlanPagoId.ToString()+ ";" + item.ImporteMora.ToString(),
                                                    item.Cargo.ToString(),
                                                    item.PagoLibre.ToString(),
                                                    item.PagoCuota.ToString(),
                                                    item.Estado + "," + item.MovimientoCajaId.ToString(),
                                                    item.FechaPagoCuota.HasValue?item.FechaPagoCuota.Value.ToShortDateString():string.Empty
                                                }
                        }
                       ).ToArray()
            };

            return Json(productsData, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult CondonarCredito(int pCreditoId, decimal pMontocxc, decimal pMontoCondonacion, string pObs)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    var cxc = CuentaxCobrarBL.Obtener(x => x.CreditoId == pCreditoId);
                    if (cxc == null)
                    {
                        cxc = CuentaxCobrarBL.Crear(new CuentaxCobrar
                        {
                            Operacion = "CDN",
                            Monto = pMontocxc,
                            Estado = "PEN",
                            CreditoId = pCreditoId
                        });
                    }
                    else
                    {
                        cxc.CreditoId = pCreditoId;
                        cxc.Operacion = "CDN";
                        cxc.Monto = pMontocxc;
                        cxc.Estado = "PEN";
                        CuentaxCobrarBL.Actualizar(cxc);
                    }

                    var c = CreditoBL.Obtener(pCreditoId);
                    c.Observacion = VendixGlobal.GetFecha().ToString() + " " + pObs;
                    c.FechaMod = VendixGlobal.GetFecha();
                    c.UsuarioModId = VendixGlobal.GetUsuarioId();
                    c.IndCondonacion = true;
                    c.MontoCondonacion = pMontoCondonacion;
                    CreditoBL.Actualizar(c);

                    var condonacion = CreditoCondonacionBL.Obtener(x => x.CreditoId == pCreditoId);
                    if (condonacion != null)
                    {
                        condonacion.IndAprobado = true;
                        CreditoCondonacionBL.Actualizar(condonacion);
                        CajaDiarioBL.RealizarPagarCuentaxCobrar(0, cxc.CuentaxCobrarId, condonacion.CajaDiarioId);
                    }
                    scope.Complete();
                    return Json(true);
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return Json(ex.InnerException.Message);
                }
            }
        }
        [HttpPost]
        public ActionResult CambiarAnalista(int pCreditoId, int pAnalistaId)
        {
            var c = CreditoBL.Obtener(pCreditoId);
            c.UsuarioRegId = pAnalistaId;
            CreditoBL.Actualizar(c);
            return Json(true);
        }
        [HttpPost]
        public ActionResult ObservarCredito(int pCreditoId, string pObs)
        {
            var c = CreditoBL.Obtener(pCreditoId);
            c.Observacion = pObs;
            CreditoBL.Actualizar(c);
            return Json(true);
            //using (var scope = new TransactionScope())
            //{
            //    try
            //    {
            //        CreditoBL.ActualizarParcial(new Credito { CreditoId = pCreditoId, Observacion = pObs }, x => x.Observacion);
            //        scope.Complete();
            //        return Json(true);
            //    }
            //    catch (Exception ex)
            //    {
            //        scope.Dispose();
            //        return Json(ex.InnerException.Message);
            //    }
            //}
        }
        [HttpPost]
        public ActionResult ProrrogarCredito(int pCreditoId, int pDias)
        {
            CreditoBL.ProrrogarCredito(pCreditoId, pDias);
            return Json(true);
        }
        [HttpPost]
        public ActionResult ActualizarTopeCredito(int pPersonaId, decimal pTopeCredito)
        {
            CreditoBL.ActualizarTopeCredito(pPersonaId, pTopeCredito);
            return Json(true);
        }
        public ActionResult ModificarTramiteAdmCredito(int pCreditoId, decimal pValor)
        {
            var c = CreditoBL.Obtener(pCreditoId);
            c.MontoGastosAdm = pValor;
            CreditoBL.ActualizarParcial(c, x => x.MontoGastosAdm);
            return Json(true);
        }
        public ActionResult ActualizarCreditoIrrecuperable(int pCreditoId, bool pIndIrrecupable)
        {
            var c = CreditoBL.Obtener(pCreditoId);
            c.IndIrrecuperable = pIndIrrecupable;
            CreditoBL.ActualizarParcial(c, x => x.IndIrrecuperable);
            return Json(pIndIrrecupable, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ModificarCentralRieagoCredito(int pCreditoId, decimal pValor)
        {
            var c = CreditoBL.Obtener(pCreditoId);
            c.CentralRiesgo = pValor;
            CreditoBL.ActualizarParcial(c, x => x.CentralRiesgo);
            return Json(true);
        }
        public ActionResult ObtenerPlanPago(int pPlanPagoId)
        {
            return Json(Repositorio<PlanPago>.Obtener(pPlanPagoId), JsonRequestBehavior.AllowGet);
        }
        public ActionResult ActualizarDescuentoPlanPago(int pPlanPagoId, decimal pDescuento)
        {
            var c = PlanPagoBL.Obtener(pPlanPagoId);
            c.Descuento = pDescuento;
            PlanPagoBL.ActualizarParcial(c, x => x.Descuento);
            return Json(true);
        }
        public ActionResult CobroBloque()
        {
            var cajadiarioid = VendixGlobal<int>.Obtener("CajadiarioId");
            if (cajadiarioid <= 0)
            {
                return Content("<h2 style='font-family:Segoe UI;color:#0D5CA3;text-align:center;margin-top:50px'>" +
                                "No tiene una Caja Diario ABIERTA asignada. Cierre esta pestaña.</h2>");
            }

            ViewBag.cboTipoPago = new SelectList(ValorTablaBL.Listar(x => x.TablaId == 13 && x.ItemId > 0), "ItemId", "Denominacion");
            ViewBag.FechaHoraServidor = VendixGlobal.GetFecha().ToString("yyyy-MM-ddTHH:mm");
            return View();
        }
        #region Cajadiario
        public ActionResult CajaDiario()
        {
            var oficinaId = VendixGlobal.GetOficinaId();

            // ViewBag.cboBovedas = new SelectList(BovedaBL.ListaBovedasXOficina(oficinaId), "BovedaId", "Denominacion");

            var usuarioId = VendixGlobal.GetUsuarioId();
            var cajadiario = CajaDiarioBL.Obtener(x => x.UsuarioAsignadoId == usuarioId && x.IndCierre == false, includeProperties: "Caja");
            if (cajadiario != null)
            {
                VendixGlobal<int>.Crear("CajadiarioId", cajadiario.CajaDiarioId);
                var listacajas = CajaBL.ListarCajasAbiertas();
                ViewBag.cboCajas = new SelectList(listacajas.Where(x => x.id != cajadiario.CajaId), "id", "value");

                var cajacentral = CajaDiarioBL.Obtener(x => x.CajaDiarioId == cajadiario.CajaDiarioId, includeProperties: "Caja").Caja.Denominacion.Contains("CAJA CENTRAL");
                ViewBag.EsCajaCentral = cajacentral ? true : false;

                var resumen = CajaDiarioBL.ObtenerResumenIngresoCajaDiario(cajadiario.CajaDiarioId);
                ViewBag.ResumenCuentaCajadiario = resumen;
            }
            ViewBag.cboTipoOperacion = new SelectList(TipoOperacionBL.Listar(x => x.IndCajaDiario), "TipoOperacionId", "Denominacion");
            var tipopago = new SelectList(ValorTablaBL.Listar(x => x.TablaId == 13 && x.ItemId > 0), "ItemId", "Denominacion");
            ViewBag.cboTipoPago = tipopago;
            ViewBag.cboTipoPagoEI = tipopago;

            return View(cajadiario);

            //return View(new DatosCajaDiario() { CajaDiario = cajadiario, Entradas = resumen[0], Salidas = resumen[1] });
        }

        public ActionResult ExisteCxCPendientes(int pPersonaId)
        {
            var usuarioid = VendixGlobal.GetUsuarioId();
            var nro = OrdenVentaBL.Contar(x => x.PersonaId == pPersonaId && x.TipoVenta == "CON" && x.Estado == "ENV");
            if (pPersonaId == 0)
                nro = nro + CuentaxCobrarBL.Contar(x => x.Credito.UsuarioRegId == usuarioid && x.Estado == "PEN");
            else
                nro = nro + CuentaxCobrarBL.Contar(x => x.Credito.PersonaId == pPersonaId && x.Estado == "PEN");


            return Json(nro, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ListarCuentasxCobrarJGrid(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = CajaDiarioBL.LstCuentasxCobrarJGrid(request, ref totalRecords);

            var data = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstItem
                        select new
                        {
                            id = item.Id,
                            cell = new string[] {
                                                    item.OrdenVentaId.ToString(),
                                                    item.CuentaxCobrarId.ToString(),
                                                    item.Codigo,
                                                    item.Cliente,
                                                    ObtenerOp(item.Operacion),
                                                    item.Origen,
                                                    item.Monto.ToString(),
                                                    item.Estado,
                                                    item.FechaReg.ToString()
                                                }
                        }
                       ).ToArray()
            };
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarDesembolsoJGrid(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = CajaDiarioBL.LstDesembolsoJGrid(request, ref totalRecords);

            var data = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstItem
                        select new
                        {
                            id = item.CreditoId,
                            cell = new string[] {
                                                    item.CreditoId.ToString(),
                                                    item.Persona.Codigo,
                                                    item.Persona.NombreCompleto,
                                                    item.MontoCredito.ToString(),
                                                    item.MontoGastosAdm.ToString(),
                                                    item.MontoDesembolso.ToString(),
                                                    item.Estado
                                                }
                        }
                       ).ToArray()
            };
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        private String ObtenerOp(string pOp)
        {
            switch (pOp)
            {
                case "CON": return "PAGO AL CONTADO";
                case "INI": return "PAGO INICIAL";
            }
            return pOp;
        }

        public ActionResult ListarCuotasPendientesJGrid(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = CajaDiarioBL.LstCuotasPendientesJGrid(request, ref totalRecords);

            var data = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstItem
                        select new
                        {
                            id = item.PlanPagoId,
                            cell = new string[] {
                                                    item.PlanPagoId.ToString(),
                                                    item.Glosa,
                                                    item.FechaVencimiento.Value.ToShortDateString(),
                                                    item.Amortizacion.ToString(),
                                                    item.Interes.ToString(),
                                                    item.GastosAdm.ToString(),
                                                    item.Cuota.ToString(),
                                                    item.DiasAtrazo.ToString(),
                                                    item.ImporteMora.ToString(),
                                                    item.Cargo.ToString(),
                                                    item.PagoLibre.ToString(),
                                                    item.PagoCuota.ToString()
                                                }
                        }
                       ).ToArray()
            };
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        //public ActionResult ObtenerTotalesCuotasPendientes()
        //{
        //    return Json(VendixGlobal<string>.Obtener("TotalesCuotasPendientes"), JsonRequestBehavior.AllowGet);
        //}

        public async Task<ActionResult> PagarCuotas(int pCreditoId, string pPlanPago, decimal pImporteRecibido)
        {
            return Json(CajaDiarioBL.PagarCuotas(VendixGlobal.GetCajaDiarioId(), pCreditoId, pPlanPago, pImporteRecibido),
                     JsonRequestBehavior.AllowGet);
        }

        public ActionResult TieneCxcPendiente(int pCreditoId)
        {
            var nro = CuentaxCobrarBL.Contar(x => x.CreditoId == pCreditoId && x.Estado == "PEN");
            return Json(nro > 0, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> PagarCuotasImporteLibre(int pCreditoId, decimal pImporteLibre, int pTipoPagoId, string pFechaHoraTrans)
        {
            return Json(CajaDiarioBL.PagarCuotaPagoLibre(VendixGlobal.GetCajaDiarioId(), pCreditoId, pImporteLibre, pTipoPagoId, pFechaHoraTrans),
                     JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> CompletarImpagos()
        {
            return Json(CreditoBL.CompletarImpagos(), JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> PagarCuotasCancelacion(int pCreditoId)
        {
            return Json(CajaDiarioBL.PagarCuotasCancelacion(VendixGlobal.GetCajaDiarioId(), pCreditoId),
                     JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> ObtenerCajaDiario()
        {
            var cajadiarioid = VendixGlobal<int>.Obtener("CajadiarioId");
            return Json(CajaDiarioBL.Obtener(cajadiarioid), JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> ObtenerResumenCuentaCajaDiario()
        {
            var cajadiarioid = VendixGlobal<int>.Obtener("CajadiarioId");
            return Json(CajaDiarioBL.ObtenerResumenIngresoCajaDiario(cajadiarioid), JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> RealizarEntradaSalidaCajaDiario(int pPersonaId, int pTipoOperacionId,
                    string pDescripcion, decimal pImporte, int pTipoPagoId)
        {
            return Json(CajaDiarioBL.EntradaSalida(pPersonaId, pTipoOperacionId, pDescripcion, pImporte, pTipoPagoId)
                    , JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> RealizarDesembolso(int pCreditoId)
        {
            return Json(CajaDiarioBL.RealizarDesembolso(pCreditoId)
                    , JsonRequestBehavior.AllowGet);
        }
        public ActionResult ValidarDesembolso(int pCreditoId)
        {
            if (CuentaxCobrarBL.Contar(x => x.CreditoId == pCreditoId && x.Estado == "PEN") > 0)
                return Json(new { error = true, mensaje = "Tiene Cuentas por cobrar Pendientes!" }, JsonRequestBehavior.AllowGet);

            var pImporte = CreditoBL.Obtener(pCreditoId).MontoDesembolso;
            if (pImporte > CajaDiarioBL.Obtener(VendixGlobal.GetCajaDiarioId()).SaldoFinal)
                return Json(new { error = true, mensaje = "Saldo Insuficiente!" }, JsonRequestBehavior.AllowGet);

            return Json(new { error = false }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RealizarPagarCuentaxCobrar(int pOrdenVentaId, int pCuentaxCobrarId)
        {
            return Json(CajaDiarioBL.RealizarPagarCuentaxCobrar(pOrdenVentaId, pCuentaxCobrarId), JsonRequestBehavior.AllowGet);
        }

        public ActionResult CerrarCajaDiario()
        {
            return Json(CajaDiarioBL.CerrarCajaDiario(), JsonRequestBehavior.AllowGet);
        }
        public ActionResult ConciliarCajaDiario()
        {
            return Json(CajaDiarioBL.ConciliarCajaDiario(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult ValidarCierreCajaDiario()
        {
            var oCajadiario = CajaDiarioBL.Obtener(VendixGlobal.GetCajaDiarioId());
            var cxc = CuentaxCobrarBL.Contar(x => x.Estado == "PEN" && x.Credito.UsuarioRegId == oCajadiario.UsuarioAsignadoId);
            if (cxc > 0)
                return Json("Tiene Cobranzas Pendientes", JsonRequestBehavior.AllowGet);

            var despendientes = CreditoBL.Contar(x => x.Estado == "APR" && x.UsuarioRegId == oCajadiario.UsuarioAsignadoId);
            if (despendientes > 0)
                return Json("Tiene Desembolsos pendientes", JsonRequestBehavior.AllowGet);

            var apropendientes = CreditoBL.Contar(x => x.Estado == "PEN" && x.UsuarioRegId == oCajadiario.UsuarioAsignadoId);
            if (apropendientes > 0)
                return Json("Tiene Creditos por Aprobar Pendientes", JsonRequestBehavior.AllowGet);

            var condonciones = CreditoCondonacionBL.Contar(x => x.IndAprobado == false && x.CajaDiarioId == oCajadiario.CajaDiarioId);
            if (condonciones > 0)
                return Json("Tiene Condonaciones Pendientes", JsonRequestBehavior.AllowGet);

            var pagosNoVerificados = CreditoBL.ValidarPagosNoVerificados();
            if (pagosNoVerificados > 0)
                return Json("Tiene Pagos no Verificados Yape, Plin o transferencias Pendientes", JsonRequestBehavior.AllowGet);

            var impagos = CreditoBL.CompletarImpagosValidar();
            if (impagos > 0)
                return Json("Tiene Creditos Impagos Pendientes", JsonRequestBehavior.AllowGet);

           
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ListarCreditosPendientesCombo(int pPersonaId)
        {
            var userid = VendixGlobal.GetUsuarioId();
            var cajadiarioId = VendixGlobal.GetCajaDiarioId();
            var caja = CajaDiarioBL.Obtener(x => x.CajaDiarioId == cajadiarioId, includeProperties: "Caja").Caja.Denominacion;
            if (caja.Contains("CAJA CENTRAL"))
            {
                return Json(
                     CreditoBL.Listar(x => x.Estado == "DES" && x.PersonaId == pPersonaId)
                            .Select(x => new
                            {
                                Id = x.CreditoId,
                                Valor = "Credito " + x.CreditoId.ToString() + " - " + x.Descripcion.Replace("\"", ""),
                                MontoCredito = x.MontoCredito
                            })
                    , JsonRequestBehavior.AllowGet);
            }

            return Json(
                CreditoBL.Listar(x => x.Estado == "DES" && x.PersonaId == pPersonaId && x.UsuarioRegId == userid)
                        .Select(x => new
                        {
                            Id = x.CreditoId,
                            Valor = "Credito " + x.CreditoId.ToString() + " - " + x.Descripcion.Replace("\"", ""),
                            MontoCredito = x.MontoCredito
                        })
                , JsonRequestBehavior.AllowGet);
        }

        public ActionResult ListarMovimientosCajaJGrid(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = CajaDiarioBL.LstMovimientosCajaJGrid(request, ref totalRecords);
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
                                                    item.CajaDiarioId.ToString(),
                                                    item.FechaReg.ToString(),
                                                    item.IndEntrada?"ENTRADA":"SALIDA",
                                                    item.Persona,
                                                    item.Operacion,
                                                    item.Descripcion,
                                                    item.ImportePago.ToString(),
                                                    item.Estado?"ACTIVO":"ANULADO",
                                                    item.Estado?item.MovimientoCajaId.ToString():""
                                                }
                        }
                       ).ToArray()
            };
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ValidarAnularMovimientoCaja(int pMovimientoCajaId)
        {
            var operacion = MovimientoCajaBL.Obtener(pMovimientoCajaId).Operacion;
            if (operacion == "INI")
            {
                int pCreditoId = CuentaxCobrarBL.Obtener(x => x.MovimientoCajaId == pMovimientoCajaId).CreditoId.Value;
                if (PlanPagoBL.Contar(x => x.Estado == "PAG" && x.CreditoId == pCreditoId) > 0)
                    return Json(true, JsonRequestBehavior.AllowGet);
            }

            return Json(false, JsonRequestBehavior.AllowGet);
        }
        public ActionResult AnularMovimientoCaja(int pMovimientoCajaId, string pObservacion)
        {
            return Json(CajaDiarioBL.AnularMovimientoCaja(pMovimientoCajaId, pObservacion), JsonRequestBehavior.AllowGet);
        }
        public ActionResult ExisteDesembolso(int pPersonaId)
        {
            return Json(CreditoBL.Contar(x => x.PersonaId == pPersonaId && x.Estado == "APR"), JsonRequestBehavior.AllowGet);
        }
        #endregion
    }

    public class DatosCredito
    {
        public Persona Persona { get; set; }
        public Cliente Cliente { get; set; }
        public int TotalCreditos { get; set; }
        public string EstadoCliente { get; set; }
        public string Analista { get; set; }
        public string CalificacionCliente { get; set; }
        public Credito SolicitudCredito { get; set; }
        public Producto Producto { get; set; }
        public List<Credito> Creditos { get; set; }
        public string ActividadEconomica { get; set; }
        public string EstadoCivil { get; set; }
        public string TipoVivienda { get; set; }
        public bool lectura { get; set; }
        public string Conyugue { get; set; }
        public string ConyugueDni { get; set; }
        public string ConyugueCelular { get; set; }
        public List<usp_RptAval_Result> Avales { get; set; }
        public string ClasificacionRiesgoSBS { get; set; }
        public string ClasificacionRiesgoSBSObs { get; set; }

    }
}
