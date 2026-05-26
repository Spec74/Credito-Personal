using System.Linq;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class TarjetaPuntoBL : Repositorio<TarjetaPunto>
   {

       public static string CanjearPuntos(int pCodCliente, string pNumSerie)
       {
           using (var db = new VENDIXEntities())
           {
               return db.usp_CanjearPuntos(pCodCliente, pNumSerie).ToList()[0];
           }
       }

      }

 }

