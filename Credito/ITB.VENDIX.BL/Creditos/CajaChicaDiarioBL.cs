using ITB.VENDIX.DA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace ITB.VENDIX.BL
{
    public class CajaChicaDiarioBL: Repositorio<CajaChicaDiario>
    {
        public static List<CajaChicaDiario> LstSaldosCajaChicaDiarioJGrid(GridDataRequest request, ref int pTotalItems)
        {          

            using (var db = new VENDIXEntities())
            {
                IQueryable<CajaChicaDiario> query = db.CajaChicaDiario.Include("Usuario");

                
                pTotalItems = query.Count();
                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }
        public static string EntradaSalida(int pPersonaId, int pTipoOperacionId, string pDescripcion, decimal pImporte)
        {

            if (string.IsNullOrEmpty(pDescripcion))
                return "Ingrese Descripción";

            var usuarioId = VendixGlobal.GetUsuarioId();
            var cajadiario = CajaChicaDiarioBL.Obtener(x => x.UsuarioId == usuarioId && x.IndCierre == false);
                        
            var tipooperacion = TipoOperacionBL.Obtener(pTipoOperacionId);
            if (!tipooperacion.IndEntrada)
            {
                if (pImporte > cajadiario.SaldoFinal)
                    return "Saldo Insuficiente!";
            }

            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_EntradaSalidaCajaChicaDiario(cajadiario.Id, pPersonaId, pTipoOperacionId, pImporte,
                                                       pDescripcion, usuarioId);
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
        public static List<MovimientoCajaChicaDiario> LstMovimientosCajaChicaJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string filterExpression = request.DataFilters()["Tipo"] == "E" ? "IndEntrada" : "IndEntrada==false";
            var usuarioId = VendixGlobal.GetUsuarioId();
            var cajadiario = CajaChicaDiarioBL.Obtener(x => x.UsuarioId == usuarioId && x.IndCierre == false);

            filterExpression += " &&  CajaChicaDiarioId == " + cajadiario.Id.ToString();

            using (var db = new VENDIXEntities())
            {
                IQueryable<MovimientoCajaChicaDiario> query = from mc in db.MovimientoCajaChica
                                                         join op in db.TipoOperacion on mc.Operacion equals op.Codigo
                                                         select new MovimientoCajaChicaDiario
                                                         {
                                                             MovimientoCajaChicaId = mc.Id,
                                                             CajaChicaDiarioId = mc.CajaChicaDiarioId,
                                                             FechaReg = mc.FechaReg,
                                                             IndEntrada = mc.IndEntrada,
                                                             Persona =
                                                                            mc.Persona == null
                                                                                ? ""
                                                                                : mc.Persona.NombreCompleto,
                                                             Operacion = op.Denominacion,
                                                             ImportePago = mc.Importe,
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
        public static List<MovimientoCajaChica> LstRendiconesPendientesJGrid(GridDataRequest request, ref int pTotalItems)
        {
            var usuarioId = VendixGlobal.GetUsuarioId();
            var cajadiario = CajaChicaDiarioBL.Obtener(x => x.UsuarioId == usuarioId && x.IndCierre == false);

            
            using (var db = new VENDIXEntities())
            {
                IQueryable<MovimientoCajaChica> qry = db.MovimientoCajaChica.Include("Persona")
                    .Where(x => x.CajaChicaDiarioId == cajadiario.Id && x.Operacion == "GAS" && x.Estado && x.IndRendido == false);
                                   

                pTotalItems = qry.Count();
                var lista = qry.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();

                return lista;
            }
        }
        public static List<MovimientoRendidoCajaChica> LstRendiconesJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string filterExpression = string.Empty;
            if (request.DataFilters()["MovimientoCajaChicaId"] != string.Empty)
                filterExpression = "MovimientoCajaChicaId=" + request.DataFilters()["MovimientoCajaChicaId"];


            using (var db = new VENDIXEntities())
            {
                IQueryable<MovimientoRendidoCajaChica> qry = db.MovimientoRendidoCajaChica.Include("TipoDocumento");
                if (!String.IsNullOrEmpty(filterExpression))
                    qry = qry.Where(filterExpression);

                pTotalItems = qry.Count();
                var lista = qry.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();

                return lista;
            }
        }
        public static int CerrarCajaChicaDiario()
        {
            var usuarioId = VendixGlobal.GetUsuarioId();
            var oCajadiario = CajaChicaDiarioBL.Obtener(x => x.UsuarioId == usuarioId && x.IndCierre == false);
            
            oCajadiario.IndCierre = true;
            oCajadiario.FechaFinOperacion = VendixGlobal.GetFecha();
            

            var oCaja = CajaBL.Obtener(x=> x.Denominacion.Contains("CAJA CHICA"));
            oCaja.IndAbierto = false;
            oCaja.FechaMod = VendixGlobal.GetFecha();
            oCaja.UsuarioModId = VendixGlobal.GetUsuarioId();



            using (var scope = new TransactionScope())
            {
                try
                {
                    Actualizar(oCajadiario);
                    CajaBL.Actualizar(oCaja);

                    scope.Complete();
                    return oCajadiario.Id;
                }
                catch (Exception)
                {
                    scope.Dispose();
                    return 0;
                }
            }
        }
        public static bool TransferirCajaChicaDiarioBoveda()
        {
            var idOficina = VendixGlobal.GetOficinaId();
            using (var scope = new TransactionScope())
            {
                try
                {
                    var cajasDiarios = Listar(x => x.IndCierre && x.TransBoveda == false );
                    var oBoveda = BovedaBL.Obtener(x => x.OficinaId == idOficina && x.IndCierre == false);

                    foreach (var item in cajasDiarios)
                    {
                        item.TransBoveda = true;
                        Actualizar(item);

                        var movBoveda = new BovedaMov
                        {
                            BovedaId = oBoveda.BovedaId,
                            CodOperacion = "TRE",
                            Glosa = "CIERRE CAJA CHICA " + VendixGlobal.GetFecha().ToShortDateString(),
                            Importe = item.SaldoFinal,
                            IndEntrada = true,
                            Estado = true,
                            UsuarioRegId = VendixGlobal.GetUsuarioId(),
                            FechaReg = VendixGlobal.GetFecha(),
                            CajaDiarioId = item.Id,
                        };
                        BovedaMovBL.Crear(movBoveda);
                    }

                    var oBovedaMov = BovedaMovBL.Listar(x => x.BovedaId == oBoveda.BovedaId && x.Estado);
                    oBoveda.Entradas = oBovedaMov.Where(x => x.IndEntrada).Sum(x => x.Importe);
                    oBoveda.Salidas = oBovedaMov.Where(x => x.IndEntrada == false).Sum(x => x.Importe);
                    oBoveda.SaldoFinal = oBoveda.SaldoInicial + oBoveda.Entradas - oBoveda.Salidas;
                    BovedaBL.Actualizar(oBoveda);
                                        
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
        public static List<usp_RptSaldosCaja_Result> ReporteSaldoCajaChicaDiario(int pCajaChicaDiarioId)
        {
            using (var bd = new VENDIXEntities())
            {
                return bd.usp_RptSaldosCaja(pCajaChicaDiarioId,true).ToList();
            }
        }
        public static ReporteSaldoCajaCab ObtenerRptSaldoCajaChicaCab(int pCajaDiarioId)
        {           
            using (var db = new VENDIXEntities())
            {
                var oficina = db.Caja.Include("Oficina").First(x => x.Denominacion.Contains("CAJA CHICA")).Oficina.Denominacion;
                var query = from mc in db.CajaChicaDiario
                            where mc.Id == pCajaDiarioId
                            select new ReporteSaldoCajaCab
                            {
                                Oficina = oficina,
                                Cajero = mc.Usuario.NombreUsuario + " - " + mc.Usuario.Persona.NombreCompleto + " - CAJA CHICA" ,
                                Estado = (mc.IndCierre ? "CERRADO" : "ABIERTO"),
                                Fecha = mc.FechaIniOperacion,
                                SaldoInicial = mc.SaldoInicial,
                                SaldoFinal = mc.SaldoFinal,
                                PorcentajeCobro = 0
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
                        var oCajaDiario = db.CajaChicaDiario.First(x => x.Id == pCajaDiarioId);
                        var oficinaPersonaId = db.Oficina.First(x => x.OficinaId == pOficinaId).Usuario.PersonaId;
                        db.MovimientoCajaChica.Add(new MovimientoCajaChica
                        {
                            CajaChicaDiarioId = pCajaDiarioId,
                            Operacion = "TRS",
                            Importe = pMonto,
                            Descripcion = "TRANS A BOVEDA: " + pDescripcion,
                            IndEntrada = false,
                            Estado = true,
                            PersonaId = oficinaPersonaId,
                            UsuarioRegId = pUsuarioRegId,
                            FechaReg = VendixGlobal.GetFecha()
                        });

                        db.BovedaMov.Add(new BovedaMov
                        {
                            BovedaId = pBovedaId,
                            CodOperacion = "TRE",
                            Glosa = "TRANS DE CAJA CHICA: " + pDescripcion,
                            Importe = pMonto,
                            IndEntrada = true,
                            Estado = true,
                            CajaDiarioId = pCajaDiarioId,
                            UsuarioRegId = pUsuarioRegId,
                            FechaReg = VendixGlobal.GetFecha()
                        });
                        db.SaveChanges();

                        var qry = db.MovimientoCajaChica.Where(z => z.CajaChicaDiarioId == oCajaDiario.Id && z.Estado).Select(x => new { x.Importe, x.IndEntrada });
                        if (qry.Count(x => x.IndEntrada) > 0)
                            oCajaDiario.Entradas = qry.Where(z => z.IndEntrada).Sum(x => x.Importe);
                        if (qry.Count(x => x.IndEntrada == false) > 0)
                            oCajaDiario.Salidas = qry.Where(z => z.IndEntrada == false).Sum(x => x.Importe);

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

        public class MovimientoCajaChicaDiario
        {
            public int MovimientoCajaChicaId { get; set; }
            public int CajaChicaDiarioId { get; set; }
            public DateTime FechaReg { get; set; }
            public string Persona { get; set; }
            public bool IndEntrada { get; set; }
            public string Operacion { get; set; }
            public decimal ImportePago { get; set; }
            public string Descripcion { get; set; }
            public bool Estado { get; set; }
        }
    }
}
