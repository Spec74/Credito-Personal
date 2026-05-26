using System.Collections.Generic;
using System.Linq;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class MenuBL:Repositorio<Menu>
    {
        public static List<usp_MenuLst_Result> ListaMenuDinamico ()
        {
            var oficinaId = VendixGlobal<int>.Obtener("OficinaId");
            var usuarioId = VendixGlobal<int>.Obtener("UsuarioId");
            using (var db = new VENDIXEntities())
            {
                
                return db.usp_MenuLst(oficinaId, usuarioId).ToList();
            }
        }
    }
}
