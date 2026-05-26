using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class BovedaBL: Repositorio<Boveda>
    {
        public static List<Boveda> LstBovedaJGrid(GridDataRequest request, ref int pTotalItems)
        {
            var oficinaid = VendixGlobal.GetOficinaId();
            using (var db = new VENDIXEntities())
            {
                IQueryable<Boveda> query = db.Boveda.Where(x=>x.OficinaId== oficinaid);             

                pTotalItems = query.Count();
                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }
        public static List<BovedaMov> LstBovedaMovJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string filterExpression = string.Empty;

            if (request.DataFilters()["BovedaId"] != string.Empty)
                filterExpression = "BovedaId == " + request.DataFilters()["BovedaId"];
            
            using (var db = new VENDIXEntities())
            {                
                IQueryable<BovedaMov> query = db.BovedaMov;
                if (!String.IsNullOrEmpty(filterExpression))
                    query = query.Where(filterExpression);

                pTotalItems = query.Count();
                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }

        public  static bool Cerrar()
        {
            var oficinaid = VendixGlobal.GetOficinaId();
            var usuarioid = VendixGlobal.GetUsuarioId();
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_CerrarBoveda(oficinaid, usuarioid);
                        var boveda = db.Boveda.FirstOrDefault(x => x.OficinaId == oficinaid && x.IndCierre == false && x.IndTemporal==false);
                        VendixGlobal<int>.Crear("BovedaId", boveda.BovedaId);
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

        public static bool CerrarBovedaTemporal()
        {
            var oficinaid = VendixGlobal.GetOficinaId();            
            var usuarioRegId = VendixGlobal.GetUsuarioId();
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_CerrarBovedaTemporal(oficinaid, usuarioRegId);
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

        public static List<usp_RptMovimientoBoveda_Result> ReporteMovimientoBoveda(int? pBovedaId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptMovimientoBoveda(pBovedaId).ToList();
            }
        }
        public static string ResumenCuentaBoveda()
        {
            var bovedaId = VendixGlobal.GetBovedaId();
            using (var db = new VENDIXEntities())
            {
                return db.usp_ResumenCuentaBoveda(bovedaId).ToList()[0];
            }
        }

        public static decimal? MontoPendientePlanPago(int pOficinaId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_ObtenerMontoPendientePlanPago(pOficinaId).ToList()[0];
            }
        }
        //public static decimal MontoVencido(int pOficinaId)
        //{
        //    var fecha = VendixGlobal.GetFecha().Date;
        //    using (var db = new VENDIXEntities())
        //    {
        //        var plan = db.PlanPago.Include("Credito").Where(x => x.Estado == "PEN" && x.Credito.Estado == "DES"
        //                                  && x.Credito.FechaVencimiento < fecha && x.Credito.OficinaId == pOficinaId).ToList();
        //        if (plan == null) return 0;
        //        return plan.Sum(x => x.Cuota - x.PagoLibre);
        //    }
        //}
        public static uspCreditoVencido_Result CreditoVencido(int? pCreditoId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.uspCreditoVencido(pCreditoId).ToList()[0];
            }
        }
    }
}
