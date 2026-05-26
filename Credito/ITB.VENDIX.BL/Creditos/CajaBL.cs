using System;
using System.Collections.Generic;
using System.Linq;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class CajaBL : Repositorio<Caja>
    {

        public static List<CajaDto> LstCajaJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string filterExpression = string.Empty;

            if (request.DataFilters()["Buscar"] != string.Empty)
                filterExpression = "Denominacion.Contains( \"" + request.DataFilters()["Buscar"] + "\")";

            using (var db = new VENDIXEntities())
            {
                IQueryable<CajaDto> query = from c in db.Caja
                                            join u in db.Usuario on c.CajeroId equals u.UsuarioId into ps
                                            from u in ps.DefaultIfEmpty()
                                            select new CajaDto
                                            {
                                                CajaId = c.CajaId,
                                                Denominacion = c.Denominacion,
                                                OficinaId = c.OficinaId,
                                                NombreOficina = c.Oficina.Denominacion,
                                                Estado = c.Estado,
                                                CajeroId = c.CajeroId,
                                                NombreCajero = u == null ? "" : u.Persona.NombreCompleto
                                            };
                                        
                if (!String.IsNullOrEmpty(filterExpression))
                    query = query.Where(filterExpression);

                pTotalItems = query.Count();
                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }
        public static List<usp_RptCajasAsignadas_Result> LstCajaDiarioOficina()
        {
            var oficina = VendixGlobal.GetOficinaId();
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptCajasAsignadas(oficina).ToList();                
            }
        }

        public static List<usp_UsuariosNoAsignadosCaja_Result> ListaUsuariosNoAsignado()
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_UsuariosNoAsignadosCaja(VendixGlobal.GetOficinaId()).ToList();
            }
        }

        public static List<ItemCombo> ListarCajasAbiertas()
        {
            var oficinaId = VendixGlobal.GetOficinaId();
            using (var db = new VENDIXEntities())
            {
                var query = from c in db.CajaDiario
                            where c.Caja.OficinaId == oficinaId && c.IndCierre == false
                            select new ItemCombo
                            {
                                id = c.CajaId,
                                value = c.Caja.Denominacion + " - " + c.Usuario.Persona.NombreCompleto
                            };
                return query.ToList();
            }
        }

    }

    public class CajaDiarioOficina : CajaDiario
    {
        public string NombreCaja { get; set; }
        public string Cajero { get; set; }
    }
    public class CajaDto : Caja
    {
        public string NombreOficina { get; set; }
        public string NombreCajero { get; set; }
    }
}
