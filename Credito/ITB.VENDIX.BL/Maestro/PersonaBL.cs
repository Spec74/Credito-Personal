
using System;
using System.Collections.Generic;
using System.Linq;
using ITB.VENDIX.DA;


namespace ITB.VENDIX.BL
{
    public class PersonaBL : Repositorio<Persona>
    {
        public static List<Persona> LstPersonaJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string filterExpression = string.Empty;

            if (request.DataFilters()["Buscar"] != string.Empty)
                filterExpression = "Denominacion.Contains( \"" + request.DataFilters()["Buscar"] + "\")";

            using (var db = new VENDIXEntities())
            {
                IQueryable<Persona> query = db.Persona;
                if (!String.IsNullOrEmpty(filterExpression))
                    query = query.Where(filterExpression);

                pTotalItems = query.Count();

                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }

        public static List<Promotores> ListarAnalista()
        {
            using (var db = new VENDIXEntities())
            {

                var query = from p in db.Persona
                            join u in db.Usuario on p.PersonaId equals u.PersonaId
                            join ur in db.UsuarioRol on u.UsuarioId equals ur.UsuarioId
                            join r in db.Rol on ur.RolId equals r.RolId
                            where r.Denominacion.Contains("ANALISTA")
                            select new Promotores
                            {
                                PersonaId = p.PersonaId,
                                NombreCompleto = p.NombreCompleto
                            };

                return query.ToList();
            }
        }

        public class Promotores
        {
            public int PersonaId { get; set; }
            public string NombreCompleto { get; set; }
        }




    }
}
