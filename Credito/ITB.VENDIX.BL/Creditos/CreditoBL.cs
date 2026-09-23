using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using ITB.VENDIX.DA;
using System.Data.Entity;

namespace ITB.VENDIX.BL
{
    public class CreditoBL : Repositorio<Credito>
    {
        public static bool CrearSolicitudCredito(int pPersonaId)
        {
            int? avalId = null;
            using (var db = new VENDIXEntities())
            {
                var aval = db.Credito.Where(x => x.PersonaId == pPersonaId && x.Estado!="CRE" && x.Estado != "ANU")
                    .OrderByDescending(x => x.CreditoId).Take(1).ToList();
                if (aval.Count > 0)
                    avalId = aval[0].PersonaAvalId;
            }
            var oCredito = new Credito
            {
                OficinaId = VendixGlobal.GetOficinaId(),
                PersonaId = pPersonaId,
                TipoCuota = "F",
                Descripcion = "",
                MontoProducto = 0,
                MontoInicial = 0,
                MontoCredito = 500,
                ProductoId = 1,
                MontoGastosAdm = 0,
                TipoGastoAdm = "CAP",
                Estado = "CRE",
                FormaPago = "D",
                NumeroCuotas = 26,
                Interes = 8,
                Observacion = string.Empty,
                FechaPrimerPago = VendixGlobal.GetFecha(),
                FechaVencimiento = VendixGlobal.GetFecha(),
                FechaReg = VendixGlobal.GetFecha(),
                UsuarioRegId = VendixGlobal.GetUsuarioId(),
                Calificacion = "A",
                PersonaAvalId = avalId
            };
            oCredito.MontoGastosAdm = GastosAdmBL.CalcularGastosAdm(oCredito.MontoCredito, true);
            CreditoBL.Crear(oCredito);
            return true;
        }

        public static DatoCredito ObtenerDatoCredito(int pCreditoId)
        {
            var data = new DatoCredito();

            using (var db = new VENDIXEntities())
            {
               var c = db.Credito.Find(pCreditoId);

                data.CreditoId = c.CreditoId;
                data.Descripcion = c.Descripcion;
                data.MontoProducto = c.MontoProducto;
                data.MontoInicial = c.MontoInicial;
                data.MontoCredito = c.MontoCredito;
                data.MontoGastosAdm = c.MontoGastosAdm;
                data.CentralRiesgo = c.CentralRiesgo;
                data.MontoDesembolso = c.MontoDesembolso;
                data.TipoGastoAdm = c.TipoGastoAdm;
                data.FormaPago = c.FormaPago;
                data.NumeroCuotas = c.NumeroCuotas;
                data.Interes = c.Interes;
                data.Estado = c.Estado;
                data.Observacion = c.Observacion;
                data.FechaDesembolso = c.FechaDesembolso;
                data.FechaAprobacion = c.FechaAprobacion;
                data.FechaVencimiento = c.FechaVencimiento;
                data.FechaPrimerPago = c.FechaPrimerPago;
                data.UsuarioRegId = c.UsuarioRegId;
                data.Analista = c.Usuario.Persona.NombreCompleto;
                data.ProductoCre = c.Producto.Denominacion;
                data.PersonaAvalId = c.PersonaAvalId;
                data.IndIrrecuperable = c.IndIrrecuperable;              


                //data.Desembolso = data.FechaDesembolso.HasValue ? data.FechaDesembolso.Value.ToShortDateString() : string.Empty;
                data.Vencimiento = data.FechaVencimiento.ToShortDateString();
                data.FPrimerPago = data.FechaPrimerPago.ToShortDateString();
                data.FAprobacion = data.FechaAprobacion.HasValue ? data.FechaAprobacion.Value.ToShortDateString() : string.Empty;
                if (data.FechaAprobacion.HasValue)
                {
                    var apro = db.Aprobacion.Include("Usuario").First(x => x.CreditoId == pCreditoId);
                    data.FAprobacion += " " + apro.Usuario.NombreUsuario;
                }
                var desembolso = db.MovimientoCaja.Include("Usuario").FirstOrDefault(x => x.CreditoId == pCreditoId && x.Operacion == "DES" && x.Estado);
                if (desembolso != null)
                    data.Desembolso = desembolso.FechaReg.ToShortDateString() + " " + desembolso.Usuario.NombreUsuario ;
                                
                var Listacargo = db.Cargo.Where(x => x.CreditoId == pCreditoId).ToList();
                if (Listacargo != null)
                    data.Cargos = Listacargo.Sum(x => x.Importe);
                //if (data.Estado == "DES")
                //    data.SaldoCancelacion = ObtenerSaldoCancelacion(pCreditoId);

                if (data.PersonaAvalId.HasValue)
                {
                    var aval = PersonaBL.Obtener(x => x.PersonaId == data.PersonaAvalId.Value);
                    data.Aval = aval.NumeroDocumento + " " + aval.NombreCompleto;
                }

                //var vencido = BovedaBL.CreditoVencido(pCreditoId);
                //data.EstadoVencido = "note";//gris si no esta desembolsado
                //if (vencido.CreditoVencido == 0)
                //    data.EstadoVencido = "success";// verde
                //if (vencido.VencidoMenor60 > 0)
                //    data.EstadoVencido = "warning";//azul
                //if (vencido.VencidoMayor60 > 0)
                //    data.EstadoVencido = "error";// naranja
                ////if (vencido.VencidoIrrecuperable > 0)
                ////    data.EstadoVencido = "error"; //rojo               

                return data;
            }
        }

        public static List<usp_SimuladorCredito_Result> SimuladorCredito
            (decimal pMonto, string pFormaPago, int pNumerocuotas, decimal pInteres, DateTime pFechaPrimerPago,
             Decimal? pGastosAdm)
        {

            using (var db = new VENDIXEntities())
            {
                return db.usp_SimuladorCredito(pMonto, pFormaPago, pNumerocuotas, pInteres,
                        pFechaPrimerPago, pGastosAdm).ToList();
            }

        }
        public static bool ProrrogarCredito(int pCreditoId, int pDias)
        {
            using (var db = new VENDIXEntities())
            {
                db.usp_ProrrogarCredito(pCreditoId, pDias);
                return true;
            }
        }
        public static bool ActualizarTopeCredito(int pPersonaId, decimal pTopeCredito)
        {
            var item = ClienteBL.Obtener(pPersonaId);
            item.TopeCredito = pTopeCredito;
            ClienteBL.ActualizarParcial(item, x => x.TopeCredito);
            return true;
        }

        public static List<RptPlanPago> ReportePlanPago(int pCreditoId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.PlanPago.Where(x => x.CreditoId == pCreditoId)
                .Select(x => new RptPlanPago
                {
                    Numero = x.Numero,
                    Capital = x.Capital,
                    FechaPago = x.FechaVencimiento,
                    Amortizacion = x.Amortizacion,
                    Interes = x.Interes,
                    GastosAdm = x.GastosAdm,
                    Cuota = x.Cuota
                }).ToList();
            }
        }
        public static List<usp_RptAval_Result> ReporteAval(int pPersonalId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptAval(pPersonalId).ToList();
            }
        }
        public static List<RptCreditoObservado> ReporteClientesNuevosMes(int? pOficinaId, int? pUsuarioId,
            DateTime? FechaInicio, DateTime? FechaFin)
        {
            if (FechaInicio.HasValue)
                FechaInicio = FechaInicio.Value.Date;
            if (FechaFin.HasValue)
                FechaFin = FechaFin.Value.Date;

            using (var db = new VENDIXEntities())
            {
                if (!FechaInicio.HasValue)
                {
                    var fecha = VendixGlobal.GetFecha().Date;
                    FechaInicio = fecha.AddDays(-fecha.Day + 1);
                    FechaFin = FechaInicio.Value.AddMonths(1).AddDays(-1);
                }
                FechaFin = FechaFin.Value.AddDays(1);
                var qry = from c in db.Credito
                          join cl in db.Cliente on c.PersonaId equals cl.PersonaId
                          where cl.FechaRegistro >= FechaInicio && cl.FechaRegistro <= FechaFin
                          &&  c.Estado == "DES"
                          select new RptCreditoObservado
                          {
                              OficinaId = c.OficinaId,
                              Oficina = c.Oficina.Denominacion,
                              CreditoId = c.CreditoId,
                              Cliente = c.Persona.NombreCompleto,
                              FechaPrimerPago = c.FechaPrimerPago,
                              FechaVencimiento = c.FechaVencimiento,
                              MontoCredito = c.MontoCredito,
                              Interes = c.Interes,
                              AgenteId = c.UsuarioRegId,
                              Agente = c.Usuario.Persona.NombreCompleto,
                              Observacion = c.Observacion,
                              TramiteAdm = c.MontoGastosAdm,
                              CentralRiesgo = c.CentralRiesgo
                          };

                if (pOficinaId.HasValue)
                    qry = qry.Where(x => x.OficinaId == pOficinaId.Value);

                if (pUsuarioId.HasValue)
                    qry = qry.Where(x => x.AgenteId == pUsuarioId.Value);

                return qry.OrderBy(x => x.Agente).ThenBy(x => x.Cliente).ToList();
            }
        }

        public static List<usp_RptCajaDiario_Result> ReporteCajaDiario(int? pOficinaId, int? pUsuarioId, DateTime? pFechaInicio, DateTime? pFechaFin)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptCajaDiario(pUsuarioId, pOficinaId, pFechaInicio, pFechaFin).ToList();
            }
        }

        public static List<usp_RptClientesInactivos_Result> ReporteClientesInactivos(int? pOficinaId, int? pUsuarioId, DateTime? pFechaInicio, DateTime? pFechaFin)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptClientesInactivos(pUsuarioId, pOficinaId, pFechaInicio, pFechaFin).ToList();
            }
        }
        public static List<usp_RptClientesBloqueados_Result> ReporteClientesBloqueados(int? pOficinaId, int? pUsuarioId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptClientesBloqueados(pOficinaId,pUsuarioId).ToList();
            }
        }
        public static List<usp_RptClientesTopeCredito_Result> ReporteClientesTopeCredito(int? pOficinaId, int? pUsuarioId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptClientesTopeCredito(pOficinaId, pUsuarioId).ToList();
            }
        }

        public static List<RptCreditoObservado> ReporteCreditoObservado(int? pOficinaId, int? pUsuarioId)
        {
            using (var db = new VENDIXEntities())
            {
                var qry = from c in db.Credito
                          where c.Observacion.Length > 0 && (c.Estado == "PEN" || c.Estado == "DES")
                          select new RptCreditoObservado
                          {
                              OficinaId = c.OficinaId,
                              Oficina = c.Oficina.Denominacion,
                              CreditoId = c.CreditoId,
                              Cliente = c.Persona.NombreCompleto,
                              FechaPrimerPago = c.FechaPrimerPago,
                              FechaVencimiento = c.FechaVencimiento,
                              MontoCredito = c.MontoCredito,
                              Interes = c.Interes,
                              AgenteId = c.UsuarioRegId,
                              Agente = c.Usuario.Persona.NombreCompleto,
                              Observacion = c.Observacion,
                              TramiteAdm = c.MontoGastosAdm,
                              CentralRiesgo = c.CentralRiesgo
                          };

                if (pOficinaId.HasValue)
                    qry = qry.Where(x => x.OficinaId == pOficinaId.Value);

                if (pUsuarioId.HasValue)
                    qry = qry.Where(x => x.AgenteId == pUsuarioId.Value);

                return qry.ToList();
            }
        }
        public static List<RptCreditoCondonado> ReporteCreditoCondonado(int? pOficinaId, int? pUsuarioId, DateTime FechaIni, DateTime FechaFin)
        {
            FechaFin = FechaFin.AddDays(1);
            using (var db = new VENDIXEntities())
            {
                var qry = from c in db.Credito
                          where c.Estado == "PAG" && c.IndCondonacion
                          && c.FechaMod >= FechaIni && c.FechaMod < FechaFin
                          select new RptCreditoCondonado
                          {
                              OficinaId = c.OficinaId,
                              Oficina = c.Oficina.Denominacion,
                              CreditoId = c.CreditoId,
                              Cliente = c.Persona.NombreCompleto,
                              FechaPrimerPago = c.FechaPrimerPago,
                              FechaVencimiento = c.FechaVencimiento,
                              MontoCredito = c.MontoCredito,
                              Interes = c.Interes,
                              MontoCondonado = c.MontoCondonacion,
                              AgenteId = c.UsuarioRegId,
                              Agente = c.Usuario.Persona.NombreCompleto,
                              Observacion = c.Observacion
                          };

                if (pOficinaId.HasValue)
                    qry = qry.Where(x => x.OficinaId == pOficinaId.Value);

                if (pUsuarioId.HasValue)
                    qry = qry.Where(x => x.AgenteId == pUsuarioId.Value);

                return qry.ToList();
            }
        }

        public static List<usp_RptCreditosActivos_Result> ReporteCreditoActivo(int? pOficinaId, int? pUsuarioId, DateTime FechaIni, DateTime FechaFin)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptCreditosActivos(FechaIni, FechaFin, pUsuarioId, pOficinaId).ToList();
            }
        }
        public static List<usp_RptCreditosMorososPagados_Result> ReporteCreditoMorosoPagado(int? pOficinaId, int? pUsuarioId, DateTime FechaIni, DateTime FechaFin)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptCreditosMorososPagados(pUsuarioId, pOficinaId, FechaIni, FechaFin).ToList();
            }
        }
        public static List<usp_RptCreditosCierres_Result> ReporteCreditoCierre(int? pOficinaId, int? pUsuarioId, DateTime FechaIni, DateTime FechaFin)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptCreditosCierres(FechaIni, FechaFin, pUsuarioId, pOficinaId).ToList();
            }
        }
        public static string CrearCredito(int pSolicitudCreditoId, int pProductoId, string pTipoCuota,
                                          decimal pMontoInicial, decimal pMontoGastosAdm, string pIndGastosAdm, decimal pMontoCredito,
                                          string pModalidad, int pNumerocuotas, decimal pInteresMensual, DateTime pFechaPrimerPago, string pObservacion,
                                          bool pIndCentralRiesgo)
        {

            string retorno;
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        retorno =
                            db.usp_Credito_Ins(pSolicitudCreditoId, pProductoId, pTipoCuota, pMontoInicial, pMontoCredito,
                                               pMontoGastosAdm, pIndGastosAdm, pModalidad, pNumerocuotas, pInteresMensual,
                                               pFechaPrimerPago, pObservacion, VendixGlobal.GetUsuarioId(), pIndCentralRiesgo).ToList()[0];
                    }
                    scope.Complete();
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    retorno = ex.GetBaseException().Message;
                }
            }
            return retorno;
        }
        public static bool AprobarCredito(int pCreditoId, int pOpcion)
        {
            var usuario = VendixGlobal<int>.Obtener("UsuarioId");
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_Credito_Upd(pOpcion, pCreditoId, usuario);
                    }
                    scope.Complete();
                    return true;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    throw;
                }
            }
        }

        public static bool CompletarImpagos()
        {
            var cajadiarioid = VendixGlobal.GetCajaDiarioId();
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_CompletarImpagos(cajadiarioid);
                    }
                    scope.Complete();
                    return true;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    throw;
                }
            }
        }
        public static int CompletarImpagosValidar()
        {
            var cajadiarioid = VendixGlobal.GetCajaDiarioId();
            using (var db = new VENDIXEntities())
            {
                return db.usp_CompletarImpagosValidacion(cajadiarioid).ToList()[0].Value;
            }
        }
        public static int ValidarPagosNoVerificados()
        {
            var cajadiarioid = VendixGlobal.GetCajaDiarioId();
            using (var db = new VENDIXEntities())
            {
                var qry = from c in db.MovimientoCaja
                          join e in db.MovimientoCajaExtension on c.MovimientoCajaId equals e.MovimientoCajaId
                          where c.CajaDiarioId == cajadiarioid && c.Estado && c.TipoPagoId > 1
                          && e.IndTransferenciaVerificada == false
                          select 1;

                return qry.Count();
            }
        }

        public static bool RechazarCredito(int pCreditoId)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        var oCredito = db.Credito.Find(pCreditoId);
                        if (oCredito.OrdenVentaId.HasValue)
                            db.usp_OrdenVenta_Del(oCredito.OrdenVentaId, 0);
                        else
                            db.usp_SolicitudCredito_Del(pCreditoId);
                    }
                    scope.Complete();
                    return true;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    throw;
                }
            }
        }
        public static bool AnularCredito(int pCreditoId, string pObservacion)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_Credito_Del(pCreditoId, pObservacion, VendixGlobal.GetUsuarioId());
                    }
                    scope.Complete();
                    return true;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    throw;
                }
            }
        }
        public static bool DepurarCredito(int pPersonaId, string pObservacion)
        {
            PersonaDepuradoBL.Crear(new PersonaDepurado
            {
                Descripcion = pObservacion,
                PersonaId = pPersonaId,
                Estado = true,
                UsuarioRegId = VendixGlobal.GetUsuarioId(),
                FechaReg = VendixGlobal.GetFecha()
            });
            return true;
        }
        public static bool ActualizarAvalCredito(int pCreditoId, int? pPersonaId)
        {
            var item = CreditoBL.Obtener(pCreditoId);
            item.PersonaAvalId = pPersonaId;
            CreditoBL.ActualizarParcial(item, x => x.PersonaAvalId);
            return true;
        }
        public static bool ReprogramarCredito(int pCreditoId)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_ReprogramarCredito(pCreditoId, VendixGlobal.GetUsuarioId());
                    }
                    scope.Complete();
                    return true;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    throw;
                }
            }
        }
        public static List<Credito> ListarCreditosGrd(GridDataRequest request, ref int pTotalItems)
        {
            string filterExpression = string.Empty;
            var oficinaid = VendixGlobal.GetOficinaId();

            if (request.DataFilters()["Buscar"] != string.Empty)
                filterExpression = "PersonaId=" + request.DataFilters()["Buscar"];

            if (request.DataFilters()["Estado"] == "DES")
                filterExpression += " && (Estado=\"PEN\" || Estado=\"APR\" || Estado=\"DES\")";
            else
                filterExpression += " && (Estado=\"ANU\" || Estado=\"PAG\" || Estado=\"REP\")";

            using (var db = new VENDIXEntities())
            {
                IQueryable<Credito> query = db.Credito.Where(x => x.OficinaId == oficinaid);
                if (!String.IsNullOrEmpty(filterExpression))
                    query = query.Where(filterExpression);

                pTotalItems = query.Count();

                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();

            }
        }
        public static List<usp_EstadoPlanPago_Result> ListarEstadoPlanPago(int pCreditoId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_EstadoPlanPago(pCreditoId).ToList();
            }
        }
        public static List<usp_RptCreditoRentabilidad_Result> ReporteCreditoRentabilidad(int? pOficinaId, DateTime pFechaIni, DateTime pFechaFin, string pEstadoCredito)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptCreditoRentabilidad(pOficinaId, pFechaIni, pFechaFin, pEstadoCredito).ToList();
            }
        }
        public static List<usp_RptCreditoAprobacion_Result> ReporteCreditoAprobacion(DateTime pFecha, int? pUsuarioid, int? pOficinaid)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptCreditoAprobacion(pFecha, pUsuarioid, pOficinaid).ToList();
            }
        }
        public static List<usp_RptCreditoMorosidad_Result> ReporteCreditoMorosidad(int? pOficinaId, DateTime pFechaHasta, int pDiasAtrazoIni, int pDiasAtrazoFin)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptCreditoMorosidad(pOficinaId, pFechaHasta, pDiasAtrazoIni, pDiasAtrazoFin).ToList();
            }
        }
        public static List<usp_RptCobroDiario_Result> ReporteCobroDiario(
            int? pGestorid,
            int? pOficinaId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptCobroDiario(
                        pGestorid,
                        pOficinaId,
                        null)
                    .ToList();
            }
        }

        public static List<usp_RptCobroDiarioDetalle_Result> ReporteCobranzaGestor(int? pGestorid, int? pOficinaId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptCobroDiarioDetalle(pGestorid, pOficinaId).ToList();
            }
        }
        public static List<usp_RptMovimientoCredito_Result> ReporteCreditoMovimiento(int pCreditoId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptMovimientoCredito(pCreditoId).ToList();
                
            }
        }
        public static List<usp_RptCreditoVencido_Result> ReporteCreditoVencido(string pVencidoMenor60, string pVencidoMayor60, string pVencidoIrrecuperable)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptCreditoVencido(pVencidoMenor60, pVencidoMayor60, pVencidoIrrecuperable).ToList();
            }
        }
        public static decimal ObtenerSaldoCancelacion(int pCreditoId)
        {
            using (var db = new VENDIXEntities())
            {
                var cancel = db.usp_CuotasPendientes(pCreditoId, VendixGlobal.GetFecha(), true).Sum(x => x.PagoCuota);
                return (decimal)cancel;
            }
        }
        public static decimal ObtenerTEM(decimal pTEA, string pFormaPago)
        {

            using (var db = new VENDIXEntities())
            {
                decimal tem = db.usp_CalcularTEM(pTEA, pFormaPago).First().Value;
                return tem;
            }
        }
        public static DateTime ObtenerFechaBD()
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_FechaBD().First().Value;
            }
        }
        public static int ObtenerDiasRetraso(DateTime FechaVencimiento)
        {
            var _fechaActual = VendixGlobal.GetFecha().Date;
            int diasRetraso = 0;
            using (var db = new VENDIXEntities())
            {
                diasRetraso = (int)db.Database.SqlQuery<int>("SELECT dbo.ufnCalcularDiasAtrazo(@p0, @p1)", FechaVencimiento, _fechaActual).FirstOrDefault();
            }
            return diasRetraso;
        }
        public static List<CreditoxAprobar> LstCreditoAprobarJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string filterExpression = string.Empty;

            if (request.DataFilters()["Buscar"] != string.Empty)
                filterExpression = "Cliente.Contains( \"" + request.DataFilters()["Buscar"] + "\")";

            using (var db = new VENDIXEntities())
            {
                IQueryable<CreditoxAprobar> query = db.Credito.Where(x => x.Estado == "PEN")
                    .Select(x => new CreditoxAprobar
                    {
                        CreditoId = x.CreditoId,
                        PersonaId = x.PersonaId,
                        Codigo = x.Persona.Codigo,
                        Cliente = x.Persona.NombreCompleto,
                        Monto = x.MontoCredito,
                        Interes = x.Interes,
                        Agente = x.Usuario.Persona.NombreCompleto
                    });
                if (!String.IsNullOrEmpty(filterExpression))
                    query = query.Where(filterExpression);

                pTotalItems = query.Count();

                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();

            }
        }

        public static List<usp_PagosNoVerificados_Result> LstVerificarPagosJGrid()
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_PagosNoVerificados().ToList();
            }
        }

    }

    public class CreditoxAprobar
    {
        public int CreditoId { get; set; }
        public int PersonaId { get; set; }
        public string Codigo { get; set; }
        public string Cliente { get; set; }
        public decimal Monto { get; set; }
        public decimal Interes { get; set; }
        public string Agente { get; set; }
    }

    public class DatoCredito : Credito
    {
        public string ProductoCre { get; set; }
        public string FPrimerPago { get; set; }
        public string FAprobacion { get; set; }
        public string Desembolso { get; set; }
        public string Vencimiento { get; set; }
        public string Analista { get; set; }
        public decimal SaldoCancelacion { get; set; }
        public decimal Cargos { get; set; }
        public string Aval { get; set; }
        public string EstadoVencido { get; set; }
    }


    public class RptPlanPago
    {
        public int Numero { get; set; }
        public decimal Capital { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal Amortizacion { get; set; }
        public decimal Interes { get; set; }
        public decimal GastosAdm { get; set; }
        public decimal Cuota { get; set; }
    }
    public class RptCreditoMov
    {
        public int MovimientoCajaId { get; set; }
        public DateTime Fecha { get; set; }
        public string Operacion { get; set; }
        public string Glosa { get; set; }
        public decimal ImportePago { get; set; }
        public decimal Saldo { get; set; }
    }
    public class RptCreditoObservado
    {
        public int OficinaId { get; set; }
        public string Oficina { get; set; }
        public int CreditoId { get; set; }
        public string Cliente { get; set; }
        public DateTime FechaPrimerPago { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal MontoCredito { get; set; }
        public decimal Interes { get; set; }
        public int AgenteId { get; set; }
        public string Agente { get; set; }
        public string Observacion { get; set; }
        public decimal TramiteAdm { get; set; }
        public decimal CentralRiesgo { get; set; }
    }
    public class RptClientesInactivos
    {
        public int PersonaId { get; set; }
        public int OficinaId { get; set; }
        public string Codigo { get; set; }
        public string DNI { get; set; }
        public string Cliente { get; set; }
        public string Celular { get; set; }
        public string Direccion { get; set; }
        public string DireccionRef { get; set; }
        public string DireccionNegocio { get; set; }
        public string DireccionNegocioRef { get; set; }
        public string Calificacion { get; set; }
        public int AgenteId { get; set; }
        public string Agente { get; set; }
    }
    public class RptCreditoCondonado
    {
        public int OficinaId { get; set; }
        public string Oficina { get; set; }
        public int CreditoId { get; set; }
        public string Cliente { get; set; }
        public DateTime FechaPrimerPago { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal MontoCredito { get; set; }
        public decimal Interes { get; set; }
        public decimal MontoCondonado { get; set; }
        public int AgenteId { get; set; }
        public string Agente { get; set; }
        public string Observacion { get; set; }
    }
}
