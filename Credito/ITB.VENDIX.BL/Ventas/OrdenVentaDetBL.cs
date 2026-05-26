using System;
using System.Linq;
using System.Transactions;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class OrdenVentaDetBL : Repositorio<OrdenVentaDet>
    {
        public static string AgregarOrdenVentaDetalle(int pOrdenVentaId, string pNumeroSerie)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    string rpta;
                    using (var db = new VENDIXEntities())
                    {
                        rpta = db.usp_OrdenVentaDet_Ins(pOrdenVentaId, pNumeroSerie, VendixGlobal.GetUsuarioId()).ToList()[0];
                    }
                    scope.Complete();
                    return rpta;
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return ex.Message;
                }
            }
        }

        public static int EliminarOrdenVentaDetalle(int pOrdenVentaDetId)
        {
            using (var db = new VENDIXEntities())
            {
                db.usp_OrdenVenta_Del(0,pOrdenVentaDetId);
            }
            return 0;
        }

        public static int ActualizarOrdenVentaDetalle(int pOrdenVentaDetId, decimal pDescuento)
        {
            using (var db = new VENDIXEntities())
            {
                db.usp_OrdenVentaDet_update(pOrdenVentaDetId, pDescuento);
            }
            return 0;
        }
    }
}

