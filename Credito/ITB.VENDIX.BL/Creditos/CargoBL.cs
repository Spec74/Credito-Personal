using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class CargoBL : Repositorio<Cargo>
    {
        public static List<CargoGrd> ListarCargoJGrid(GridDataRequest request, ref int pTotalItems)
        {
            var creditoId = Int32.Parse(request.DataFilters()["CreditoId"]);
            using (var db = new VENDIXEntities())
            {
                var qry2 = from c in db.Cargo
                           join vt in (db.ValorTabla.Where(x => x.TablaId == 2)) on c.TipoCargoT2 equals vt.ItemId
                           where c.CreditoId == creditoId
                           select new CargoGrd
                                      {
                                          CargoId = c.CargoId,
                                          TipoCargo = vt.Denominacion,
                                          NumCuota = c.NumCuota,
                                          Descripcion = c.Descripcion,
                                          Importe = c.Importe,
                                          UsuarioCargo = c.Usuario.NombreUsuario,
                                          Estado = c.Estado
                                      };
                pTotalItems = qry2.Count();

                return qry2.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }

        public static bool CrearCargo(int pCreditoId, int pTipoCargoId, decimal pMonto, string pDescripcion, bool pFinal)
        {
            var usuarioid = VendixGlobal.GetUsuarioId();
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        int numcuota;
                        if (pFinal)
                            numcuota = db.PlanPago.Where(x => x.CreditoId == pCreditoId && x.Estado == "PEN")
                                .OrderByDescending(x => x.Numero).Take(1).First().Numero;
                        else
                            numcuota = db.PlanPago.Where(x => x.CreditoId == pCreditoId && x.Estado == "PEN")
                                .OrderBy(x => x.Numero).Take(1).First().Numero;

                        Crear(db, new Cargo()
                                      {
                                          CreditoId = pCreditoId,
                                          NumCuota = numcuota,
                                          Descripcion = pDescripcion,
                                          TipoCargoT2 = pTipoCargoId,
                                          Importe = pMonto,
                                          UsuarioId = usuarioid,
                                          Fecha = VendixGlobal.GetFecha(),
                                          Estado = "PEN"
                                      });
                        db.SaveChanges();
                        var montocargo = db.Cargo.Where(x => x.CreditoId == pCreditoId && x.NumCuota == numcuota && x.Estado == "PEN").Sum(x => x.Importe);
                        var planpago = db.PlanPago.First(x => x.CreditoId == pCreditoId && x.Numero == numcuota);
                        planpago.Cargo = montocargo;
                        planpago.PagoCuota = planpago.Cuota + planpago.ImporteMora  + planpago.Cargo - planpago.PagoLibre ;
                        db.SaveChanges();
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
    }

    public class CargoGrd : Cargo
    {
        public string TipoCargo { get; set; }
        public string UsuarioCargo { get; set; }
    }
}
