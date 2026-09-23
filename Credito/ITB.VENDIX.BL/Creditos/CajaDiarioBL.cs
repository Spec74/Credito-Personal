using ITB.VENDIX.DA;
using ITB.VENDIX.BL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Transactions;
using System.Data.Entity;
using System.Threading.Tasks;
namespace ITB.VENDIX.BL
{
    public class CajaDiarioBL : Repositorio<CajaDiario>
    {
        public static List<CajaDiario> LstSaldosCajaDiarioJGrid(GridDataRequest request, ref int pTotalItems)
        {
            //string filterExpression = string.Empty;
            // var bovedaid = int.Parse(request.DataFilters()["BovedaId"]);
            var oficinaid = VendixGlobal.GetOficinaId();

            using (var db = new VENDIXEntities())
            {
                IQueryable<CajaDiario> query = db.CajaDiario.Include("Caja").Include("Usuario")
                    .Where(x => x.Caja.OficinaId == oficinaid);

                //if (!String.IsNullOrEmpty(filterExpression))
                //    query = query.Where(filterExpression);

                pTotalItems = query.Count();
                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }

        public static List<CajaDiarioBoveda> LstSaldosBovedaCajaDiarioJGrid(GridDataRequest request, ref int pTotalItems)
        {
            //string filterExpression = string.Empty;
            var bovedaid = int.Parse(request.DataFilters()["BovedaId"]);


            using (var db = new VENDIXEntities())
            {
                IQueryable<CajaDiarioBoveda> query = null;
                if (bovedaid > 0)
                {
                    query = (from cd in db.CajaDiario
                             join mb in db.BovedaMov on cd.CajaDiarioId equals mb.CajaDiarioId
                             where mb.BovedaId == bovedaid
                             select new CajaDiarioBoveda
                             {
                                 CajaDiarioId = cd.CajaDiarioId,
                                 Caja = cd.Caja.Denominacion,
                                 Usuario = cd.Usuario.NombreUsuario,
                                 SaldoInicial = cd.SaldoInicial,
                                 SaldoFinal = cd.SaldoFinal,
                                 FechaFinOperacion = cd.FechaFinOperacion,
                                 FechaIniOperacion = cd.FechaIniOperacion,
                                 IndCierre = cd.IndCierre,
                                 TransBoveda = cd.TransBoveda
                             }).Distinct();
                }

                //if (!String.IsNullOrEmpty(filterExpression))
                //    query = query.Where(filterExpression);

                pTotalItems = query.Count();
                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }
        public class CajaDiarioBoveda
        {
            public int CajaDiarioId { get; set; }
            public string Usuario { get; set; }
            public string Caja { get; set; }
            public decimal SaldoInicial { get; set; }
            public decimal SaldoFinal { get; set; }
            public DateTime FechaIniOperacion { get; set; }
            public DateTime? FechaFinOperacion { get; set; }
            public bool IndCierre { get; set; }
            public bool TransBoveda { get; set; }
        }

        public static List<CxcJgrid> LstCuentasxCobrarJGrid(GridDataRequest request, ref int pTotalItems)
        {
            var personaid = int.Parse(request.DataFilters()["PersonaId"]);
            var usuarioid = VendixGlobal.GetUsuarioId();
            var cajadiarioid = VendixGlobal.GetCajaDiarioId();
            var cajacentral = CajaDiarioBL.Obtener(x => x.CajaDiarioId == cajadiarioid, includeProperties: "Caja").Caja.Denominacion.Contains("CAJA CENTRAL");
            IQueryable<CuentaxCobrar> qrycxc;
            using (var db = new VENDIXEntities())
            {
                if (personaid == 0)
                    qrycxc = db.CuentaxCobrar.Where(x => x.Credito.UsuarioRegId == usuarioid && x.Estado == "PEN");
                else
                {
                    if (cajacentral)
                        qrycxc = db.CuentaxCobrar.Where(x => x.Credito.PersonaId == personaid && x.Estado == "PEN");
                    else
                        qrycxc = db.CuentaxCobrar.Where(x => x.Credito.PersonaId == personaid && x.Credito.UsuarioRegId == usuarioid && x.Estado == "PEN");

                }


                IQueryable<CxcJgrid> query = qrycxc
                    .Select(x => new CxcJgrid
                    {
                        OrdenVentaId = x.Credito.OrdenVentaId == null ? 0 : x.Credito.OrdenVentaId.Value,
                        CuentaxCobrarId = x.CuentaxCobrarId,
                        Codigo = x.Credito.Persona.Codigo,
                        Cliente = x.Credito.Persona.NombreCompleto,
                        Operacion = x.Operacion,
                        Origen = " CREDITO: " + x.CreditoId.ToString(),
                        Monto = x.Monto,
                        Estado = x.Estado,
                        FechaReg = x.Credito.FechaAprobacion.Value
                    })
                    .Union(db.OrdenVenta.Where(x => x.PersonaId == personaid && x.TipoVenta == "CON" && x.Estado == "ENV")
                               .Select(x => new CxcJgrid
                               {
                                   OrdenVentaId = x.OrdenVentaId,
                                   CuentaxCobrarId = 0,
                                   Codigo = x.Persona.Codigo,
                                   Cliente = x.Persona.NombreCompleto,
                                   Operacion = "CON",
                                   Origen = "ORDEN: " + x.OrdenVentaId.ToString(),
                                   Monto = x.TotalNeto,
                                   Estado = "PEN",
                                   FechaReg = x.FechaReg
                               }));

                pTotalItems = query.Count();
                var lista = query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();

                for (var i = 0; i < lista.Count(); i++)
                    lista[i].Id = i;

                return lista;
            }
        }

        public static List<Credito> LstDesembolsoJGrid(GridDataRequest request, ref int pTotalItems)
        {
            var personaid = int.Parse(request.DataFilters()["PersonaId"]);
            var usuarioid = VendixGlobal.GetUsuarioId();
            IQueryable<Credito> query;
            using (var db = new VENDIXEntities())
            {
                if (personaid == 0)
                    query = db.Credito.Include("Persona").Where(x => x.UsuarioRegId == usuarioid && x.Estado == "APR");
                else
                    query = db.Credito.Include("Persona").Where(x => x.PersonaId == personaid && x.Estado == "APR");

                pTotalItems = query.Count();
                var lista = query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();

                return lista;
            }
        }
        public static List<CreditoPendienteJGrid> LstCreditoPendienteJGrid(GridDataRequest request, ref int pTotalItems)
        {
            var usuarioid = VendixGlobal.GetUsuarioId();

            using (var db = new VENDIXEntities())
            {
                // 1. Consulta base ligera
                var query = db.Credito
                    .Where(x => x.UsuarioRegId == usuarioid && x.Estado == "DES");

                pTotalItems = query.Count();

                // 2. Ordenamiento (Usamos var + AsQueryable para que no dé error el nombre de la clase)
                var queryOrdenada = query
                    .OrderBy(x => x.FechaVencimiento)
                    .ThenBy(x => x.Persona != null ? x.Persona.NombreCompleto : "")
                    .AsQueryable();

                if (!string.IsNullOrEmpty(request.sidx) && request.sidx.Trim() != "FechaVencimiento")
                {
                    queryOrdenada = query.OrderBy(request.sidx + " " + request.sord);
                }

                int paginaSegura = Math.Max(1, request.page);

                // 3. Paginación: Traemos SOLO las filas de la página actual a memoria RAM
                var listaPaginada = queryOrdenada
                    .Skip((paginaSegura - 1) * request.rows)
                    .Take(request.rows)
                    .ToList();

                // 4. Cálculo Matemático Exacto en C#
                return listaPaginada.Select(x => {
                    var cuotasPendientes = x.PlanPago.Where(p => p.Estado != "PAG" && p.Estado != "CAN").ToList();
                    var cuotasPagadas = x.PlanPago.Where(p => p.Estado == "PAG").ToList();

                    // Interés TOTAL del crédito para que sume 2675.00 exactos (ignoramos solo anuladas)
                    decimal interesTotal = x.PlanPago
                        .Where(p => p.Estado != "CAN")
                        .Sum(p => (decimal?)p.Interes) ?? 0m;

                    // Mora Total = (Mora de cuotas pendientes + Mora histórica de cuotas pagadas)
                    decimal moraPendiente = cuotasPendientes.Sum(p => (decimal?)p.ImporteMora) ?? 0m;
                    decimal moraPagada = cuotasPagadas.Sum(p => (decimal?)p.ImporteMora) ?? 0m;
                    decimal moraTotal = moraPendiente + moraPagada;

                    // DEUDA PENDIENTE REAL DEL CLIENTE
                    decimal cuotas = cuotasPendientes.Sum(p => (decimal?)p.Cuota) ?? 0m;
                    decimal cargos = cuotasPendientes.Sum(p => (decimal?)p.Cargo) ?? 0m;
                    decimal pagoLibre = cuotasPendientes.Sum(p => (decimal?)p.PagoLibre) ?? 0m;
                    decimal descuentos = cuotasPendientes.Sum(p => (decimal?)p.Descuento) ?? 0m;

                    decimal capitalPendiente = cuotas + cargos - pagoLibre - descuentos;
                    decimal deudaTotalReal = capitalPendiente + moraTotal;

                    return new CreditoPendienteJGrid
                    {
                        CreditoId = x.CreditoId,
                        Codigo = x.Persona != null ? x.Persona.Codigo : "",
                        Cliente = x.Persona != null ? x.Persona.NombreCompleto : "",
                        MontoCredito = x.MontoCredito,
                        PersonaId = x.PersonaId,
                        FechaVencimiento = x.FechaVencimiento,

                        Interes = interesTotal,

                        ImporteMora = moraTotal,
                        DeudaPendiente = deudaTotalReal
                    };
                }).ToList();
            }
        }

        public static void ActualizarMontoPorCobrar(int usuarioid)
        {
            using (var db = new VENDIXEntities())
            {
                db.usp_CalcularMontoPorCobrar(usuarioid);
            }
        }
        public static decimal? ObtenerSaldoCuentaCajadiario(int cajaDiarioId, int tipoPagoId = 1)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_ObtenerSaldoCuentaCajadiario(cajaDiarioId, tipoPagoId).ToList()[0];
            }
        }
        public static void ActualizarSaldosCajaDiario(int usuarioid, int oficinaid)
        {
            using (var db = new VENDIXEntities())
            {
                db.usp_ActualizarSaldosCajaDiario(usuarioid, oficinaid);
            }
        }

        public static List<usp_CuotasPendientes_Result> LstCuotasPendientesJGrid(GridDataRequest request,
                                                                                 ref int pTotalItems
                                                                                 )
        {
            var indCancelacion = bool.Parse(request.DataFilters()["indCancelacion"]);

            int? creditoId = null;
            if (request.DataFilters()["CreditoId"] != null)
                creditoId = int.Parse(request.DataFilters()["CreditoId"]);

            List<usp_CuotasPendientes_Result> lista;
            using (var db = new VENDIXEntities())
            {
                lista = db.usp_CuotasPendientes(creditoId, VendixGlobal.GetFecha(), indCancelacion).ToList();
            }

            pTotalItems = lista.Count();

            return lista.Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
        }


        public static List<MovimientoCajaDiario> LstMovimientosCajaJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string filterExpression = request.DataFilters()["Tipo"] == "E" ? "IndEntrada" : "IndEntrada==false";
            filterExpression += " &&  CajaDiarioId == " + VendixGlobal<int>.Obtener("CajadiarioId").ToString();

            using (var db = new VENDIXEntities())
            {
                IQueryable<MovimientoCajaDiario> query = from mc in db.MovimientoCaja
                                                         join op in db.TipoOperacion on mc.Operacion equals op.Codigo
                                                         select new MovimientoCajaDiario
                                                         {
                                                             MovimientoCajaId = mc.MovimientoCajaId,
                                                             CajaDiarioId = mc.CajaDiarioId,
                                                             FechaReg = mc.FechaReg,
                                                             IndEntrada = mc.IndEntrada,
                                                             Persona =
                                                                            mc.Persona == null
                                                                                ? ""
                                                                                : mc.Persona.NombreCompleto,
                                                             Operacion = op.Denominacion,
                                                             ImportePago = mc.ImportePago,
                                                             Descripcion = mc.Descripcion,
                                                             Estado = mc.Estado
                                                         };
                if (!String.IsNullOrEmpty(filterExpression))
                    query = query.Where(filterExpression);

                pTotalItems = query.Count();

                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }


        public static int? PagarCuotas(int pCajaDiarioId, int pCreditoId, string pPlanPago, decimal pImporteRecibido, int pTipoPagoId = 1, string pFechaHoraTrans = "")
        {
            //using (var scope = new TransactionScope())
            //{
            try
            {
                int? retid;
                using (var db = new VENDIXEntities())
                {
                    retid = db.usp_PagarCuotas(pCajaDiarioId, pCreditoId, pPlanPago, pImporteRecibido,
                                               VendixGlobal.GetUsuarioId(), VendixGlobal.GetFecha(), pTipoPagoId, pFechaHoraTrans).ToList()[0];
                }
                //scope.Complete();
                return retid;
            }
            catch (Exception)
            {
                //scope.Dispose();
                return -1;
            }
            //}
        }
        public static int? PagarCuotaPagoLibre(int pCajaDiarioId, int pCreditoId, decimal pImporteRecibido,
                                                int pTipoPagoId = 1, string pFechaHoraTrans = "")
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    int? retid;
                    using (var db = new VENDIXEntities())
                    {
                        retid = db.usp_PagarCuotaPagoLibre(pCajaDiarioId, pCreditoId, pImporteRecibido,
                                                   VendixGlobal.GetUsuarioId(), pTipoPagoId, pFechaHoraTrans).ToList()[0];
                    }
                    scope.Complete();
                    return retid;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    return -1;
                }
            }
        }

        public class cal
        {
            public int Fila { get; set; }
            public decimal PagoCuota { get; set; }
            public int PlanPagoId { get; set; }
        }

        public static int? PagarCuotasCancelacion(int pCajaDiarioId, int pCreditoId)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    int? retid;
                    using (var db = new VENDIXEntities())
                    {
                        retid =
                            db.usp_PagarCuotasCancelacion(pCajaDiarioId, pCreditoId, VendixGlobal.GetUsuarioId(),
                                                          VendixGlobal.GetFecha()).ToList()[0];
                    }
                    scope.Complete();
                    return retid;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    return -1;
                }
            }
        }

        public static string EntradaSalida(int pPersonaId, int pTipoOperacionId, string pDescripcion,
                                    decimal pImporte, int pTipoPagoId)
        {

            if (string.IsNullOrEmpty(pDescripcion))
                return "Ingrese Descripción";

            var cajadiarioid = VendixGlobal.GetCajaDiarioId();
            var tipooperacion = TipoOperacionBL.Obtener(pTipoOperacionId);
            if (!tipooperacion.IndEntrada)
            {
                if (pImporte > Obtener(cajadiarioid).SaldoFinal)
                    return "Saldo Insuficiente!";
            }

            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_EntradaSalidaCajaDiario(cajadiarioid, pPersonaId, pTipoOperacionId, pImporte,
                                                       pDescripcion, VendixGlobal.GetUsuarioId(), pTipoPagoId);
                    }
                    scope.Complete();
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return ex.Message;
                }
            }
        }

        public static string RealizarDesembolso(int pCreditoId)
        {
            var cajadiarioid = VendixGlobal.GetCajaDiarioId();
            var usuarioid = VendixGlobal.GetUsuarioId();
            int movimientoCajaId = 0;

            var existeDesembolso = MovimientoCajaBL
                .Obtener(x => x.CreditoId == pCreditoId && x.Operacion == "DES" && x.Estado);
            if (existeDesembolso != null)
                return existeDesembolso.MovimientoCajaId.ToString();

            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        var credito = db.Credito.Find(pCreditoId);
                        credito.FechaDesembolso = VendixGlobal.GetFecha();
                        credito.Estado = "DES";
                        credito.UsuarioModId = usuarioid;
                        credito.FechaMod = VendixGlobal.GetFecha();
                        CreditoBL.Actualizar(db, credito);

                        var mov = new MovimientoCaja()
                        {
                            CajaDiarioId = cajadiarioid,
                            Operacion = "DES",
                            ImportePago = credito.MontoDesembolso,
                            PersonaId = credito.PersonaId,
                            Descripcion = "DESEMBOLSO CREDITO " + credito.CreditoId.ToString(),
                            IndEntrada = false,
                            Estado = true,
                            TipoPagoId = 1,
                            UsuarioRegId = usuarioid,
                            FechaReg = VendixGlobal.GetFecha(),
                            OrdenVentaId = null,
                            CreditoId = credito.CreditoId
                        };

                        db.MovimientoCaja.Add(mov);
                        db.SaveChanges();
                        movimientoCajaId = mov.MovimientoCajaId;

                        var oCajaDiario = db.CajaDiario.Find(cajadiarioid);
                        var qry = db.MovimientoCaja.Where(z => z.CajaDiarioId == oCajaDiario.CajaDiarioId && z.Estado).Select(x => new { x.ImportePago, x.IndEntrada });
                        if (qry.Count(x => x.IndEntrada) > 0)
                            oCajaDiario.Entradas = qry.Where(z => z.IndEntrada).Sum(x => x.ImportePago);
                        if (qry.Count(x => x.IndEntrada == false) > 0)
                            oCajaDiario.Salidas = qry.Where(z => z.IndEntrada == false).Sum(x => x.ImportePago);
                        oCajaDiario.SaldoFinal = oCajaDiario.SaldoInicial + oCajaDiario.Entradas - oCajaDiario.Salidas;

                        db.SaveChanges();

                    }
                    scope.Complete();
                    return movimientoCajaId.ToString();
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return ex.Message;
                }
            }
        }
        public static int? RealizarPagarCuentaxCobrar(int pOrdenVentaId, int pCuentaxCobrarId, int pCajaDiarioId = 0)
        {
            if (pCajaDiarioId == 0)
                pCajaDiarioId = VendixGlobal.GetCajaDiarioId();

            using (var scope = new TransactionScope())
            {
                try
                {
                    int? retid;
                    using (var db = new VENDIXEntities())
                    {
                        retid = db.usp_PagarCuentaxCobrar(pOrdenVentaId, pCuentaxCobrarId, pCajaDiarioId,
                                                      VendixGlobal.GetUsuarioId()).ToList()[0];
                    }
                    scope.Complete();
                    return retid;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    return -1;
                }
            }
        }

        public static int CerrarCajaDiario()
        {
            var oCajadiario = Obtener(VendixGlobal.GetCajaDiarioId());
            oCajadiario.IndCierre = true;
            oCajadiario.SaldoFinal = Math.Round(oCajadiario.SaldoFinal, 1);
            oCajadiario.FechaFinOperacion = VendixGlobal.GetFecha();
            var MontoCobrado = MovimientoCajaBL.Listar(x => x.CajaDiarioId == oCajadiario.CajaDiarioId && x.IndEntrada && x.Operacion == "CUO" && x.Estado)
                            .Sum(x => x.ImportePago);
            oCajadiario.MontoCobrado = MontoCobrado;

            var oCaja = CajaBL.Obtener(oCajadiario.CajaId);
            oCaja.IndAbierto = false;
            oCaja.FechaMod = VendixGlobal.GetFecha();
            oCaja.UsuarioModId = VendixGlobal.GetUsuarioId();


            using (var scope = new TransactionScope())
            {
                try
                {

                    Actualizar(oCajadiario);
                    CajaBL.Actualizar(oCaja);

                    using (var db = new VENDIXEntities())
                    {
                        db.ActualizarClientesNuevos(oCajadiario.CajaDiarioId);
                    }

                    scope.Complete();
                    return oCajadiario.CajaDiarioId;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    return 0;
                }
            }
        }
        public static int ConciliarCajaDiario()
        {
            var cajadiarioId = VendixGlobal.GetCajaDiarioId();
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_ReconciliarCajaDiario(cajadiarioId);
                    }
                    scope.Complete();
                    return cajadiarioId;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    return 0;
                }
            }
        }
        public static bool AnularMovimientoCaja(int pMovimientoCajaId, string pObservacion)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_MovimientoCaja_Del(pMovimientoCajaId, pObservacion, VendixGlobal.GetUsuarioId());
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

        public static string AsignarUsuarioCaja(int pCajaId, int pUsuarioAsignadoId, decimal pSaldoInicial)
        {
            var usuarioid = VendixGlobal.GetUsuarioId();
            var idOficina = VendixGlobal.GetOficinaId();
            var oCaja = CajaBL.Obtener(pCajaId);
            var cajaDiario = new CajaDiario();
            var indCajaChica = oCaja.Denominacion.Contains("CAJA CHICA");
            if (!oCaja.CajeroId.HasValue)
                return "No tiene adignado un Cajero.";

            var encargado = UsuarioRolBL.Contar(x => x.UsuarioId == usuarioid
                                                            && x.OficinaId == idOficina
                                                            && x.Rol.Denominacion == "ENCARGADO", includeProperties: "Rol");
            Boveda oBoveda = null;
            if (encargado > 0)
                oBoveda = BovedaBL.Obtener(x => x.OficinaId == idOficina && x.IndCierre == false && x.IndTemporal == true);
            else
                oBoveda = BovedaBL.Obtener(x => x.OficinaId == idOficina && x.IndCierre == false && x.IndTemporal == false);


            if (pSaldoInicial > oBoveda.SaldoFinal)
                return "Saldo Insuficiente de la boveda.";

            using (var scope = new TransactionScope())
            {
                try
                {
                    if (!indCajaChica)//caja diario
                    {
                        cajaDiario = new CajaDiario
                        {
                            CajaId = pCajaId,
                            UsuarioAsignadoId = oCaja.CajeroId.Value,
                            SaldoInicial = pSaldoInicial,
                            Entradas = 0,
                            Salidas = 0,
                            SaldoFinal = pSaldoInicial,
                            FechaIniOperacion = VendixGlobal.GetFecha(),
                            IndCierre = false,
                            TransBoveda = false,
                        };
                        Crear(cajaDiario);
                        ActualizarMontoPorCobrar(cajaDiario.UsuarioAsignadoId);
                        //ActualizarSaldosCajaDiario(cajaDiario.UsuarioAsignadoId, idOficina);
                    }
                    else
                    {// caja chica
                        var oCajachicadiario = new CajaChicaDiario
                        {
                            Id = pCajaId,
                            UsuarioId = oCaja.CajeroId.Value,
                            SaldoInicial = pSaldoInicial,
                            Entradas = 0,
                            Salidas = 0,
                            SaldoFinal = pSaldoInicial,
                            FechaIniOperacion = VendixGlobal.GetFecha(),
                            IndCierre = false,
                            TransBoveda = false,
                        };
                        CajaChicaDiarioBL.Crear(oCajachicadiario);
                    }

                    oCaja.IndAbierto = true;
                    CajaBL.Actualizar(oCaja);


                    if (pSaldoInicial > 0)
                    {
                        BovedaMovBL.Crear(new BovedaMov
                        {
                            BovedaId = oBoveda.BovedaId,
                            CodOperacion = "TRS",
                            Glosa = "INICIAL " + oCaja.Denominacion + " " + VendixGlobal.GetFecha().ToShortDateString(),
                            Importe = pSaldoInicial,
                            IndEntrada = false,
                            Estado = true,
                            UsuarioRegId = VendixGlobal.GetUsuarioId(),
                            FechaReg = VendixGlobal.GetFecha(),
                            CajaDiarioId = (indCajaChica ? 0 : cajaDiario.CajaDiarioId),
                            TipoPagoId = 1
                        });
                        var oBovedaMov = BovedaMovBL.Listar(x => x.BovedaId == oBoveda.BovedaId && x.Estado);
                        oBoveda.Entradas = oBovedaMov.Where(x => x.IndEntrada).Sum(x => x.Importe);
                        oBoveda.Salidas = oBovedaMov.Where(x => x.IndEntrada == false).Sum(x => x.Importe);
                        oBoveda.SaldoFinal = oBoveda.SaldoInicial + oBoveda.Entradas - oBoveda.Salidas;
                        BovedaBL.Actualizar(oBoveda);
                    }
                    scope.Complete();
                    return string.Empty;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    throw;
                }
            }
        }

        public static bool TransferirCajaDiarioBoveda(decimal pSobrante)
        {
            var idOficina = VendixGlobal.GetOficinaId();
            var idUsuario = VendixGlobal.GetUsuarioId();
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_CerrarCajasDiarios(idUsuario, idOficina, pSobrante);
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
        public static async Task ActualizarDatosPostCierreBoveda()
        {
            using (var db = new VENDIXEntities())
            {
                var idOficina = VendixGlobal.GetOficinaId();
                var idUsuario = VendixGlobal.GetUsuarioId();

                db.usp_ActualizarSaldoCartera(idOficina);
                db.usp_CalificarCliente(idOficina);

                /*
                 * La evaluacion de fecha se realiza en SQL Server mediante
                 * dbo.ufnFecha(). Fuera del ultimo dia del mes, o de la
                 * contingencia de los dias 1 y 2, el procedimiento no hace
                 * ninguna modificacion.
                 */
                var oficinaParametro = new SqlParameter("@OficinaId", idOficina);
                var usuarioParametro = new SqlParameter("@UsuarioCierreId", idUsuario);

                await db.Database.ExecuteSqlCommandAsync(
                    "EXEC CREDITO.usp_IntentarGenerarCierreGerencialMensual " +
                    "@OficinaId, @UsuarioCierreId",
                    oficinaParametro,
                    usuarioParametro);
            }
        }

        public static string MostrarDetalleOvMovCaja(int pMovimientoCajaId)
        {
            string detalleventa = string.Empty;

            using (var db = new VENDIXEntities())
            {
                var operacion = db.MovimientoCaja.Find(pMovimientoCajaId).Operacion;

                switch (operacion)
                {
                    case "INI":
                        detalleventa = string.Join(Environment.NewLine,
                                                   db.CuentaxCobrar.FirstOrDefault(
                                                       x => x.MovimientoCajaId == pMovimientoCajaId)
                                                       .Credito.OrdenVenta.OrdenVentaDet.Select(x => x.Descripcion));

                        break;
                    case "CON":
                        detalleventa = string.Join(Environment.NewLine,
                                                   db.MovimientoCaja.Find(pMovimientoCajaId)
                                                       .OrdenVenta.OrdenVentaDet.Select(x => x.Descripcion));
                        break;
                    case "CUO":
                        detalleventa = string.Join(Environment.NewLine,
                                                   db.PlanPago.FirstOrDefault(
                                                       x => x.MovimientoCajaId == pMovimientoCajaId)
                                                       .Credito.OrdenVenta.OrdenVentaDet.Select(x => x.Descripcion));
                        break;
                }
            }
            return detalleventa;
        }

        public static List<usp_RptSaldosCaja_Result> ReporteSaldoCajaDiario(int pCajaDiarioId)
        {
            using (var bd = new VENDIXEntities())
            {
                return bd.usp_RptSaldosCaja(pCajaDiarioId, false).ToList();
            }
        }

        public static string ObtenerResumenIngresoCajaDiario(int pCajaDiarioId, int pOficinaId = 1)
        {
            using (var bd = new VENDIXEntities())
            {
                return bd.usp_RptSaldosCajaResumenIngreso(pCajaDiarioId, pOficinaId).ToList()[0];
            }
        }
        public static string ObtenerResumenCuentaCajaDiarios(int pOficinaId = 1)
        {
            using (var bd = new VENDIXEntities())
            {
                return bd.usp_RptSaldosCajaResumenTipoCuenta(pOficinaId).ToList()[0];
            }
        }
        public static ReporteSaldoCajaCab ObtenerRptSaldoCajaCab(int pCajaDiarioId)
        {

            using (var db = new VENDIXEntities())
            {
                var query = from mc in db.CajaDiario
                            where mc.CajaDiarioId == pCajaDiarioId
                            select new ReporteSaldoCajaCab
                            {
                                Oficina = mc.Caja.Oficina.Denominacion,
                                Cajero = mc.Usuario.NombreUsuario + " - " + mc.Usuario.Persona.NombreCompleto + " - " + mc.Caja.Denominacion,
                                Estado = (mc.IndCierre ? "CERRADO" : "ABIERTO"),
                                Fecha = mc.FechaIniOperacion,
                                SaldoInicial = mc.SaldoInicial,
                                SaldoFinal = mc.SaldoFinal,
                                PorcentajeCobro = mc.MontoPorCobrar > 0 ? (mc.MontoCobrado * 100) / mc.MontoPorCobrar : 0
                            };
                return query.First();
            }
        }

        public static bool TransferirSaldosBoveda(decimal pMonto, string pDescripcion, int pCajaDiarioId, int pBovedaId, int pOficinaId, int pUsuarioRegId)
        {
            using (var scope = new TransactionScope())
            {
                try
                {

                    using (var db = new VENDIXEntities())
                    {
                        var oCajaDiario = db.CajaDiario.First(x => x.CajaDiarioId == pCajaDiarioId);
                        var oficinaPersonaId = db.Oficina.First(x => x.OficinaId == pOficinaId).Usuario.PersonaId;
                        var MovBoveda = new BovedaMov
                        {
                            BovedaId = pBovedaId,
                            CodOperacion = "TRE",
                            Glosa = "TRANS DE CAJA: " + pDescripcion,
                            Importe = pMonto,
                            IndEntrada = true,
                            Estado = true,
                            CajaDiarioId = pCajaDiarioId,
                            UsuarioRegId = pUsuarioRegId,
                            FechaReg = VendixGlobal.GetFecha(),
                            TipoPagoId = 1
                        };
                        BovedaMovBL.Crear(MovBoveda);

                        db.MovimientoCaja.Add(new MovimientoCaja
                        {
                            CajaDiarioId = pCajaDiarioId,
                            Operacion = "TRS",
                            ImportePago = pMonto,
                            Descripcion = "[MovBoveda:" + MovBoveda.MovimientoBovedaId.ToString() + "] TRANS A BOVEDA: " + pDescripcion,
                            IndEntrada = false,
                            Estado = true,
                            PersonaId = oficinaPersonaId,
                            TipoPagoId = 1,
                            UsuarioRegId = pUsuarioRegId,
                            FechaReg = VendixGlobal.GetFecha()
                        });

                        db.SaveChanges();

                        var qry = db.MovimientoCaja.Where(z => z.CajaDiarioId == oCajaDiario.CajaDiarioId && z.Estado).Select(x => new { x.ImportePago, x.IndEntrada });
                        if (qry.Count(x => x.IndEntrada) > 0)
                            oCajaDiario.Entradas = qry.Where(z => z.IndEntrada).Sum(x => x.ImportePago);
                        if (qry.Count(x => x.IndEntrada == false) > 0)
                            oCajaDiario.Salidas = qry.Where(z => z.IndEntrada == false).Sum(x => x.ImportePago);

                        oCajaDiario.SaldoFinal = oCajaDiario.SaldoInicial + oCajaDiario.Entradas - oCajaDiario.Salidas;

                        db.usp_ActualizarSaldosBoveda(pBovedaId);
                        db.SaveChanges();
                    }
                    scope.Complete();
                    return true;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    return false;
                }
            }

        }

        public static bool TransferirSaldosCajaDiario(decimal pMonto, string pDescripcion,
            int pCajaDiarioOrigenId, int pCajaDiarioDestinoId, int pOficinaId, int pUsuarioRegId)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        var oCajaDiario = db.CajaDiario.First(x => x.CajaDiarioId == pCajaDiarioOrigenId);
                        var oCajaDiarioDestino = db.CajaDiario.First(x => x.CajaDiarioId == pCajaDiarioDestinoId);
                        var oPersonaOrigenId = db.CajaDiario.First(x => x.CajaDiarioId == oCajaDiario.CajaDiarioId).Usuario.PersonaId;
                        var oPersonaDestinoId = db.CajaDiario.First(x => x.CajaDiarioId == oCajaDiarioDestino.CajaDiarioId).Usuario.PersonaId;
                        var sCajaOrigen = db.CajaDiario.First(x => x.CajaDiarioId == oCajaDiario.CajaDiarioId).Caja.Denominacion;
                        var sCajaDestino = db.CajaDiario.First(x => x.CajaDiarioId == oCajaDiarioDestino.CajaDiarioId).Caja.Denominacion;
                        var MovCajaOrigen = new MovimientoCaja
                        {
                            CajaDiarioId = pCajaDiarioOrigenId,
                            Operacion = "TRS",
                            ImportePago = pMonto,
                            Descripcion = "TRANS A CAJA " + sCajaDestino + ": " + pDescripcion,
                            IndEntrada = false,
                            Estado = true,
                            PersonaId = oPersonaOrigenId,
                            TipoPagoId = 1,
                            UsuarioRegId = pUsuarioRegId,
                            FechaReg = VendixGlobal.GetFecha()
                        };
                        MovimientoCajaBL.Crear(MovCajaOrigen);

                        var MovCajaDestino = new MovimientoCaja
                        {
                            CajaDiarioId = pCajaDiarioDestinoId,
                            Operacion = "TRE",
                            ImportePago = pMonto,
                            Descripcion = "[MovCajero:" + MovCajaOrigen.MovimientoCajaId.ToString() + "] TRANS DE CAJA " + sCajaOrigen + ": " + pDescripcion,
                            IndEntrada = true,
                            Estado = true,
                            TipoPagoId = 1,
                            PersonaId = oPersonaDestinoId,
                            UsuarioRegId = pUsuarioRegId,
                            FechaReg = VendixGlobal.GetFecha()
                        };
                        MovimientoCajaBL.Crear(MovCajaDestino);
                        MovCajaOrigen.Descripcion = "[MovCajero:" + MovCajaDestino.MovimientoCajaId.ToString() + "] " + MovCajaOrigen.Descripcion;
                        MovimientoCajaBL.Actualizar(db, MovCajaOrigen);
                        db.SaveChanges();


                        var qry = db.MovimientoCaja.Where(z => z.CajaDiarioId == oCajaDiario.CajaDiarioId && z.Estado)
                            .Select(x => new { x.ImportePago, x.IndEntrada });
                        if (qry.Count(x => x.IndEntrada) > 0)
                            oCajaDiario.Entradas = qry.Where(z => z.IndEntrada).Sum(x => x.ImportePago);
                        if (qry.Count(x => x.IndEntrada == false) > 0)
                            oCajaDiario.Salidas = qry.Where(z => z.IndEntrada == false).Sum(x => x.ImportePago);
                        oCajaDiario.SaldoFinal = oCajaDiario.SaldoInicial + oCajaDiario.Entradas - oCajaDiario.Salidas;
                        CajaDiarioBL.Actualizar(db, oCajaDiario);

                        qry = db.MovimientoCaja.Where(z => z.CajaDiarioId == oCajaDiarioDestino.CajaDiarioId && z.Estado)
                           .Select(x => new { x.ImportePago, x.IndEntrada });
                        if (qry.Count(x => x.IndEntrada) > 0)
                            oCajaDiarioDestino.Entradas = qry.Where(z => z.IndEntrada).Sum(x => x.ImportePago);
                        if (qry.Count(x => x.IndEntrada == false) > 0)
                            oCajaDiarioDestino.Salidas = qry.Where(z => z.IndEntrada == false).Sum(x => x.ImportePago);
                        oCajaDiarioDestino.SaldoFinal = oCajaDiarioDestino.SaldoInicial + oCajaDiarioDestino.Entradas - oCajaDiarioDestino.Salidas;
                        CajaDiarioBL.Actualizar(db, oCajaDiarioDestino);
                        db.SaveChanges();
                    }
                    scope.Complete();
                    return true;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    return false;
                }
            }

        }

        public static List<usp_RptSaldoCarteraCajaDiario_Result> ReporteSaldoCarteraCajaDiario(int? pUsuarioId, int? pOficinaId, int anioIni, int mesIni, int anioFin, int mesFin)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptSaldoCarteraCajaDiario(pUsuarioId, pOficinaId, anioIni, mesIni, anioFin, mesFin).ToList();
            }
        }
        public static List<MovimientoCaja> ListarMovimientoCajaGrd(GridDataRequest request,
                                                                                 ref int pTotalItems,
                                                                                 ref string pTotales)
        {
            int personaid = int.Parse(request.DataFilters()["PersonaId"]);

            IQueryable<MovimientoCaja> query;
            using (var db = new VENDIXEntities())
            {
                if (personaid != 0)
                    query = db.MovimientoCaja.Where(x => x.PersonaId == personaid);
                else
                    query = new List<MovimientoCaja>().AsQueryable();

                pTotalItems = query.Count();
                var lista = query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();

                return lista;
            }
        }
    }
    public class CreditoPendienteJGrid
    {
        public int CreditoId { get; set; }
        public string Codigo { get; set; }
        public string Cliente { get; set; }
        public decimal MontoCredito { get; set; }
        public int PersonaId { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public decimal DeudaPendiente { get; set; }
        public decimal Interes { get; set; }
        public decimal ImporteMora { get; set; }
    }
    public class ReporteSaldoCajaCab
    {
        public string Oficina { get; set; }
        public string Cajero { get; set; }
        public string Estado { get; set; }
        public DateTime Fecha { get; set; }
        public decimal SaldoInicial { get; set; }
        public decimal SaldoFinal { get; set; }
        public decimal PorcentajeCobro { get; set; }
    }
    public class CxcJgrid
    {
        public int Id { get; set; }
        public int OrdenVentaId { get; set; }
        public int CuentaxCobrarId { get; set; }
        public string Codigo { get; set; }
        public string Cliente { get; set; }
        public string Operacion { get; set; }
        public string Origen { get; set; }
        public string Estado { get; set; }
        public DateTime FechaReg { get; set; }
        public decimal Monto { get; set; }

    }

    public class MovimientoCajaDiario
    {
        public int MovimientoCajaId { get; set; }
        public int CajaDiarioId { get; set; }
        public DateTime FechaReg { get; set; }
        public string Persona { get; set; }
        public bool IndEntrada { get; set; }
        public string Operacion { get; set; }
        public decimal ImportePago { get; set; }
        public string Descripcion { get; set; }
        public bool Estado { get; set; }
    }

}
