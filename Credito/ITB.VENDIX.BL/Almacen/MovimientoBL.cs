using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Objects;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Transactions;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class MovimientoBL:Repositorio<Movimiento>
    {
        public static EntradaSalida ObtenerEntradaSalida(int pMovimientoId)
        {
            using (var db = new VENDIXEntities())
            {
                var qry2 = from mov in db.Movimiento
                           join vt in (db.ValorTabla.Where(x => x.TablaId == 5)) on mov.EstadoId equals vt.ItemId
                           where mov.MovimientoId == pMovimientoId
                           select new EntradaSalida
                                      {
                                         MovimientoId  = mov.MovimientoId,
                                         Oficina = mov.Almacen.Oficina.Denominacion,
                                         Almacen = mov.Almacen.Denominacion,
                                         Tipo = mov.TipoMovimiento.IndEntrada ? "ENTRADA" : "SALIDA",
                                         TipoMovimientoId = mov.TipoMovimientoId,
                                         TipoMovimiento = mov.TipoMovimiento.Denominacion,
                                         TipoMovimientoDesc = mov.TipoMovimiento.Descripcion,
                                         Fecha = mov.Fecha,
                                         Documento = mov.Documento,
                                         Estado = vt.Denominacion,
                                         Observacion = mov.Observacion,
                                         Importe = mov.TotalImporte
                                      };
                return qry2.FirstOrDefault();
            }
            
        }
        public static List<EntradaDetalle> ObtenerEntradaDetalle(int pMovimientoId)
        {
            using (var db = new VENDIXEntities())
            {
                var qry2 = from movd in db.MovimientoDet
                           join vt in (db.ValorTabla.Where(x => x.TablaId == 10)) on movd.UnidadMedidaT10 equals vt.ItemId
                           where movd.MovimientoId == pMovimientoId 
                           select new EntradaDetalle
                                      {
                                          MovimientoDetId = movd.MovimientoDetId,
                                          ArticuloId = movd.ArticuloId,
                                          Cantidad=movd.Cantidad,
                                          UnidadMedidaT10 = movd.UnidadMedidaT10,
                                          UnidadMedida = vt.DesCorta,
                                          Descripcion = movd.Descripcion,
                                          PrecioUnitario = movd.PrecioUnitario,
                                          Descuento = movd.Descuento,
                                          Importe = movd.Importe,
                                          IndCorrelativo = movd.IndCorrelativo,
                                          Elimina = movd.Movimiento.EstadoId == 1
                                      };
                return qry2.ToList();
            }

        }

        public static List<EntradaSalida> LstEntradaJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string clave = request.DataFilters().Count > 0 ? request.DataFilters()["Buscar"] : string.Empty;
            int almacenId = int.Parse(request.DataFilters()["Almacen"]);
            int articuloId = int.Parse(request.DataFilters()["BuscarxArticuloId"]);
             
            using (var db = new VENDIXEntities())
            {
                //db.Configuration.ProxyCreationEnabled = false;
                //db.Configuration.LazyLoadingEnabled = false;
                //db.Configuration.ValidateOnSaveEnabled = false;
                IQueryable<EntradaSalida> qry;
                if (articuloId==0)
                {
                    qry = from mov in db.Movimiento
                          join vt in (db.ValorTabla.Where(x => x.TablaId == 5)) on mov.EstadoId equals vt.ItemId
                          where mov.AlmacenId == almacenId && mov.TipoMovimiento.IndEntrada
                          select new EntradaSalida
                                     {
                                         MovimientoId = mov.MovimientoId,
                                         Tipo = mov.TipoMovimiento.IndEntrada ? "ENTRADA" : "SALIDA",
                                         TipoMovimientoId = mov.TipoMovimientoId,
                                         TipoMovimiento = mov.TipoMovimiento.Denominacion,
                                         Fecha = mov.Fecha,
                                         Documento = mov.Documento,
                                         Estado = vt.Denominacion,
                                         Observacion = mov.Observacion,
                                         Tags = mov.MovimientoId.ToString() + " " + mov.Documento
                                     };
                    if (clave != string.Empty)
                    {
                        DateTime fecha;
                        qry = DateTime.TryParse(clave, out fecha)
                            ? qry.Where(x => EntityFunctions.TruncateTime(x.Fecha) == fecha.Date)
                            : qry.Where("Tags.Contains(\"" + clave + "\")");
                    }
                }
                else
                {
                    qry = from dmov in db.MovimientoDet
                          join vt in (db.ValorTabla.Where(x => x.TablaId == 5)) on dmov.Movimiento.EstadoId equals vt.ItemId
                          where dmov.Movimiento.AlmacenId == almacenId && dmov.Movimiento.TipoMovimiento.IndEntrada &&
                              dmov.ArticuloId == articuloId
                          select new EntradaSalida
                                     {
                                         MovimientoId = dmov.MovimientoId,
                                         Tipo = dmov.Movimiento.TipoMovimiento.IndEntrada ? "ENTRADA" : "SALIDA",
                                         TipoMovimientoId = dmov.Movimiento.TipoMovimientoId,
                                         TipoMovimiento = dmov.Movimiento.TipoMovimiento.Denominacion,
                                         Fecha = dmov.Movimiento.Fecha,
                                         Documento = dmov.Movimiento.Documento,
                                         Estado = vt.Denominacion,
                                         Observacion = dmov.Movimiento.Observacion,
                                         Tags = dmov.Movimiento.MovimientoId.ToString() + " " +
                                             dmov.Movimiento.Documento
                                     };
                    qry = qry.Distinct();
                }

                pTotalItems = qry.Count();
                return qry.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }

        public static List<EntradaSalida> LstTransferenciaJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string clave = request.DataFilters().Count > 0 ? request.DataFilters()["Buscar"] : string.Empty;
            int almacenId = int.Parse(request.DataFilters()["Almacen"]);
            int articuloId = int.Parse(request.DataFilters()["BuscarxArticuloId"]);

            using (var db = new VENDIXEntities())
            {
                //db.Configuration.ProxyCreationEnabled = false;
                //db.Configuration.LazyLoadingEnabled = false;
                //db.Configuration.ValidateOnSaveEnabled = false;
                IQueryable<EntradaSalida> qry;
                if (articuloId == 0)
                {
                    qry = from mov in db.Movimiento
                          join vt in (db.ValorTabla.Where(x => x.TablaId == 5)) on mov.EstadoId equals vt.ItemId
                          where mov.AlmacenId == almacenId && mov.TipoMovimiento.IndTransferencia == true
                          select new EntradaSalida
                          {
                              MovimientoId = mov.MovimientoId,
                              Tipo = mov.TipoMovimiento.IndEntrada ? "ENTRADA" : "SALIDA",
                              TipoMovimientoId = mov.TipoMovimientoId,
                              TipoMovimiento = mov.TipoMovimiento.Denominacion,
                              Fecha = mov.Fecha,
                              Documento = mov.Documento,
                              Estado = vt.Denominacion,
                              Observacion = mov.Observacion,
                              Tags =mov.MovimientoId.ToString() + " " + mov.Documento
                          };
                    if (clave != string.Empty)
                    {
                        DateTime fecha;
                        qry = DateTime.TryParse(clave, out fecha)
                            ? qry.Where(x => EntityFunctions.TruncateTime(x.Fecha) == fecha.Date)
                            : qry.Where("Tags.Contains(\"" + clave + "\")");
                    }
                }
                else
                {
                    qry = from dmov in db.MovimientoDet
                          join vt in (db.ValorTabla.Where(x => x.TablaId == 5)) on dmov.Movimiento.EstadoId equals vt.ItemId
                          where dmov.Movimiento.AlmacenId == almacenId && dmov.Movimiento.TipoMovimiento.IndTransferencia == true &&
                              dmov.ArticuloId == articuloId
                          select new EntradaSalida
                          {
                              MovimientoId = dmov.MovimientoId,
                              Tipo = dmov.Movimiento.TipoMovimiento.IndEntrada ? "ENTRADA" : "SALIDA",
                              TipoMovimientoId = dmov.Movimiento.TipoMovimientoId,
                              TipoMovimiento = dmov.Movimiento.TipoMovimiento.Denominacion,
                              Fecha = dmov.Movimiento.Fecha,
                              Documento = dmov.Movimiento.Documento,
                              Estado = vt.Denominacion,
                              Observacion = dmov.Movimiento.Observacion,
                              Tags = dmov.Movimiento.MovimientoId.ToString() + " " +
                                             dmov.Movimiento.Documento
                          };
                    qry = qry.Distinct();
                }

                pTotalItems = qry.Count();
                return qry.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }
        public class EntradaSalida
        {
            public int MovimientoId { get; set; }
            public string Oficina { get; set; }
            public string Almacen { get; set; }
            public string Tipo { get; set; }
            public int TipoMovimientoId { get; set; }
            public string TipoMovimiento { get; set; }
            public string TipoMovimientoDesc { get; set; }
            public DateTime Fecha { get; set; }
            public string Documento { get; set; }
            public string Estado { get; set; }
            public string Observacion { get; set; }
            public string Tags { get; set; }
            public decimal Importe { get; set; }
            
        }
        public class EntradaDetalle:MovimientoDet
        {
            public string UnidadMedida { get; set; }
            public bool Elimina { get; set; }
        }

        public static string ConfirmarMov(int pMovimientoId)
        {
            string ret;
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                       ret= db.usp_Movimiento_Upd(3, pMovimientoId, null, null, null).ToList()[0];
                    }
                    scope.Complete();
                    return ret;
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return ex.Message;
                }
            }
        }

        public static bool ActualizarImporte(int pMovimientoId, decimal pAjusteRedondeo)
        {
            string qry = "UPDATE ALMACEN.Movimiento SET AjusteRedondeo=" + pAjusteRedondeo.ToString() +
                         ", TotalImporte = SubTotal + IGV + " + pAjusteRedondeo.ToString() +
                         " WHERE MovimientoId = " + pMovimientoId.ToString();
            return EjecutarSql(qry);
        }

        public static string Desconfirmar(int pMovimientoId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_Movimiento_Upd(1, pMovimientoId,null,null,null).ToList()[0];
            }
        }
        public static bool ActualizarMov(int pMovimientoId,int pTipoMovimientoId,DateTime pFecha, string pObservacion)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_Movimiento_Upd(2, pMovimientoId, pTipoMovimientoId, pFecha, pObservacion);
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
}
