using System;
using System.Collections.Generic;
using System.Linq;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class ClienteBL : Repositorio<Cliente>
    {
        public static List<ItemCombo> BuscarCliente(string pClave)
        {
            using (var db = new VENDIXEntities())
            {
                var qry = (from p in db.Persona
                           join c in db.Cliente on p.PersonaId equals c.PersonaId
                           where c.Estado && (p.NombreCompleto.Contains(pClave) || p.NumeroDocumento.Contains(pClave))
                           orderby p.NombreCompleto
                           select new ItemCombo { id = p.PersonaId, value = p.NumeroDocumento + " " + p.NombreCompleto + " [" + (string.IsNullOrEmpty(p.Codigo) ? "" : p.Codigo) + " ]" }).Take(10);
                return qry.ToList();
            }
        }
       
        public static List<ItemCombo> BuscarDistrito(string pClave)
        {
            using (var db = new VENDIXEntities())
            {
                var qry = (from p in db.Distrito
                           join c in db.Provincia on p.idProv equals c.idProv
                           where c.idDepa == 5 && (p.Denominacion.Contains(pClave) || c.Denominacion.Contains(pClave))
                           orderby p.Denominacion
                           select new ItemCombo { id = p.idDist, value = p.Denominacion + " - " + c.Denominacion }).Take(10);
                return qry.ToList();
            }
        }
        public static List<ItemCombo> BuscarPersona(string pClave)
        {
            using (var db = new VENDIXEntities())
            {
                var qry = from p in db.Persona
                          where p.Estado && (p.NombreCompleto.Contains(pClave) || p.NumeroDocumento.Contains(pClave))
                          orderby p.NombreCompleto
                          select new ItemCombo { id = p.PersonaId, value = p.NumeroDocumento + " " + p.NombreCompleto };
                return qry.ToList();
            }
        }

        public static List<ItemCombo> BuscarUsuario(string pClave)
        {
            using (var db = new VENDIXEntities())
            {
                var qry = from p in db.Usuario
                          where p.Estado && (p.Persona.NombreCompleto.Contains(pClave) || p.Persona.NumeroDocumento.Contains(pClave))
                          orderby p.Persona.NombreCompleto
                          select new ItemCombo { id = p.PersonaId, value = p.Persona.NumeroDocumento + " " + p.Persona.NombreCompleto };
                return qry.ToList();
            }
        }

        public static List<Clientejgrid> LstClienteJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string filterExpression = string.Empty;
            var userid = VendixGlobal.GetUsuarioId();
            if (request.DataFilters()["Buscar"] != string.Empty)
                filterExpression =
                    "Cliente.Contains( \"" + request.DataFilters()["Buscar"] + "\") || Documento.Contains( \"" + request.DataFilters()["Buscar"] + "\")";
            IQueryable<Clientejgrid> query;
            using (var db = new VENDIXEntities())
            {
                if (request.DataFilters()["Buscar"] != string.Empty)
                    query = db.Cliente.Select(x => new Clientejgrid
                    {
                        PersonaId = x.PersonaId,
                        Codigo = x.Persona.Codigo,
                        Cliente = x.Persona.NombreCompleto,
                        Documento = x.Persona.TipoDocumento + " " + x.Persona.NumeroDocumento,
                        Celular = x.Persona.Celular1,
                        Direccion = x.Persona.Direccion,
                        Email = x.Persona.Direccion
                    });
                else
                    query = db.Credito.Where(x => x.UsuarioRegId == userid).Select(x => new Clientejgrid
                    {
                        PersonaId = x.PersonaId,
                        Codigo = x.Persona.Codigo,
                        Cliente = x.Persona.NombreCompleto,
                        Documento = x.Persona.TipoDocumento + " " + x.Persona.NumeroDocumento,
                        Celular = x.Persona.Celular1,
                        Direccion = x.Persona.Direccion,
                        Email = x.Persona.EmailPersonal
                    }).Distinct();                                

                if (!String.IsNullOrEmpty(filterExpression))
                    query = query.Where(filterExpression);

                pTotalItems = query.Count();

                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }

public class Clientejgrid
        {
            public int PersonaId { get; set; }
            public string Codigo { get; set; }
            public string Cliente { get; set; }
            public string Documento { get; set; }
            public string Celular { get; set; }
            public string Email { get; set; }
            public string Direccion { get; set; }

        }
    }
}