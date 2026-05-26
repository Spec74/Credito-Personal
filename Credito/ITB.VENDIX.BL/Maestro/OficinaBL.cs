using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class OficinaBL : Repositorio<Oficina>
    {
        public static List<Oficina> LstOficinaJGrid(GridDataRequest request,ref int pTotalItems)
        {
            string filterExpression = string.Empty;

            if (request.DataFilters()["Buscar"] != string.Empty)
                filterExpression = "Denominacion.Contains( \"" + request.DataFilters()["Buscar"] + "\")";

            using (var db = new VENDIXEntities())
            {
                IQueryable<Oficina> query = db.Oficina;
                if (!String.IsNullOrEmpty(filterExpression))
                    query = query.Where(filterExpression);

                pTotalItems = query.Count();

                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1)*request.rows).Take(request.rows).ToList();

            }
        }

        public static bool CrearOficina(Oficina pOficina)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    var oficina = Crear(pOficina);
                    BovedaBL.Crear(new Boveda
                                       {
                                           OficinaId = oficina.OficinaId,
                                           SaldoInicial = 0,
                                           Entradas = 0,
                                           Salidas = 0,
                                           SaldoFinal = 0,
                                           FechaIniOperacion = VendixGlobal.GetFecha(),
                                           IndCierre = false
                                       });

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
    }
}

