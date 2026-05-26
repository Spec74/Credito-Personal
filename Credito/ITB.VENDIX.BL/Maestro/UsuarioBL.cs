using System;
using System.Collections.Generic;
using System.Linq;
using ITB.VENDIX.DA;
namespace ITB.VENDIX.BL
{
    public class UsuarioBL : Repositorio<Usuario>
    {
        public static List<Usuario> LstUsuarioJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string filterExpression = string.Empty;

            if (request.DataFilters()["Buscar"] != string.Empty)
                filterExpression = "NombreUsuario.Contains( \"" + request.DataFilters()["Buscar"] + "\")";
                //filterExpression = "NombreUsuario.Contains( \"" + request.DataFilters()["Buscar"] + "\")";
            using (var db = new VENDIXEntities())
            {
                IQueryable<Usuario> query = db.Usuario.Include("Persona");
                if (!String.IsNullOrEmpty(filterExpression))
                    query = query.Where(filterExpression);

                pTotalItems = query.Count();

                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }

        public static string ObtenerNombre(int pUsuarioId)
        {
            using (var db = new VENDIXEntities())
            {
                var query = db.Usuario.Where(x => x.UsuarioId == pUsuarioId).Select(x => new { nombre = x.Persona.NombreCompleto }).First();
                return query.nombre;
            }
        }
    }
}
