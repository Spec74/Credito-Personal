using System;
using ITB.VENDIX.DA;
using System.Transactions;

namespace ITB.VENDIX.BL
{
    public class MovimientoDetBL : Repositorio<MovimientoDet>
    {
        public static bool EliminarDetalle(int pMovimientoDetId)
        {
            using (var db = new VENDIXEntities())
            {
                db.usp_EliminarMovimientoDet(pMovimientoDetId);
                return true;
            }
        }

        public static bool CrearDetalle(int pMovimientoId, int pMovimientoDetId, int pArticuloId, bool pIndAutogenerar,
                                    string pListaSerie, int pCantidad, bool pIndCorrelativo, decimal pPrecioUnitario, decimal pDescuento, int pMedida)
        {
            using (var db = new VENDIXEntities())
            {
                db.usp_CrearMovimientoDet(pMovimientoId, pMovimientoDetId, pArticuloId, pIndAutogenerar, pListaSerie, pCantidad,
                                          pIndCorrelativo, pPrecioUnitario, pDescuento, pMedida);
            }
            return true;
        }

        public static object AgregarDetalleTranferencia(string pNumeroSerie, int pMovimientoId)
        {


            using (var scope = new TransactionScope())
            {
                try
                {
                    //string rpta;
                    
                        var a = SerieArticuloBL.Obtener(x => x.NumeroSerie == pNumeroSerie,includeProperties:"Articulo").Articulo;

                       // MovimientoDetBL.Crear(new MovimientoDet { ArticuloId = a.ArticuloId, Cantidad=1,Descripcion=a.Descripcion,Descuento= });

                   
                    scope.Complete();
                    return true;
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return ex.Message;
                }
            }
        }

    }
}
