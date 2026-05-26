using ITB.VENDIX.DA;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Objects;
using System.Linq;
using System.Transactions;

namespace ITB.VENDIX.BL
{
    public class TransferenciaBL : Repositorio<Transferencia>
    {

        //public static bool AgregarTransferenciaSerie(Transferencia t , List<int> series) {

        //    foreach (var c in series)
        //        t.SerieArticulo.Add(new SerieArticulo { SerieArticuloId = c });

        //    using ( var db= new VENDIXEntities()) {

        //        if (t.TransferenciaId == 0)
        //        {
        //            db.Entry(t).State = EntityState.Added;
        //        }
        //        else
        //        {
        //            db.Database.ExecuteSqlCommand(
        //                "DELETE FROM TransferenciaSerie WHERE TransferenciaId = @id",
        //                new SqlParameter("id", t.TransferenciaId)
        //            );

        //            var serieBK = t.SerieArticulo;

        //            t.SerieArticulo = null;
        //            db.Entry(t).State = EntityState.Modified;
        //            t.SerieArticulo = serieBK;
        //        }

        //        foreach (var c in t.SerieArticulo)
        //            db.Entry(c).State = EntityState.Unchanged;

        //        db.SaveChanges();

        //    }


        //    return true;
        //}
                
        public class EntradaSalida
        {
            public int TransferenciaId { get; set; }
            public string AlmacenOrigen { get; set; }
            public string AlmacenDestino { get; set; }
            public string UsuarioId { get; set; }
            public DateTime Fecha { get; set; }
            public string Estado { get; set; }

        }
                        
        public static List<EntradaSalida> LstTransferenciaJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string clave = request.DataFilters().Count > 0 ? request.DataFilters()["Buscar"] : string.Empty;
            int almacenId = int.Parse(request.DataFilters()["Almacen"]);
            int articuloId = int.Parse(request.DataFilters()["BuscarxArticuloId"]);
            int ofinaId = VendixGlobal.GetOficinaId();

            using (var db = new VENDIXEntities())
            {
                //db.Configuration.ProxyCreationEnabled = false;
                //db.Configuration.LazyLoadingEnabled = false;
                //db.Configuration.ValidateOnSaveEnabled = false;
                IQueryable<EntradaSalida> qry;
               
                    qry = from tra in db.Transferencia
                          where tra.Almacen.OficinaId == ofinaId || tra.Almacen1.OficinaId == ofinaId 
                          select new EntradaSalida
                          {
                              TransferenciaId = tra.TransferenciaId,
                              AlmacenOrigen = tra.Almacen.Denominacion,
                              AlmacenDestino = tra.Almacen1.Denominacion,
                              Fecha = tra.Fecha,
                              Estado = tra.Estado
                          };
                    if (clave != string.Empty)
                    {
                        DateTime fecha;
                        qry = DateTime.TryParse(clave, out fecha)
                            ? qry.Where(x => EntityFunctions.TruncateTime(x.Fecha) == fecha.Date)
                            : qry.Where("TransferenciaId=" + clave );
                    }
            
                

                pTotalItems = qry.Count();
                return qry.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }

        //public static List<EntradaDetalle> ObtenerEntradaDetalle(int pTransferenciaId)
        //{
        //    using (var db = new VENDIXEntities())
        //    {
        //        db.Configuration.ProxyCreationEnabled = false;
        //        db.Configuration.LazyLoadingEnabled = false;
        //        db.Configuration.ValidateOnSaveEnabled = false;

        //        var qry2 = (from s in db.SerieArticulo
        //                  // where s.Transferencia..Contains(pTransferenciaId)
        //                   select new EntradaDetalle
        //                   {
        //                       TransferenciaId = pTransferenciaId,
        //                       SerieArticuloId = s.SerieArticuloId,
        //                       NumeroSerie = s.NumeroSerie,
        //                       Articulo = s.Articulo.Denominacion                               
        //                   }).Take(3);
        //        return qry2.ToList();
        //    }

        //}
        public static EntradaSalida ObtenerEntradaSalida(int pTransferenciaId)
        {
            using (var db = new VENDIXEntities())
            {
                var qry2 = from tra in db.Transferencia
                           join al in db.Almacen on tra.AlmacenOrigenId equals al.AlmacenId
                           join al2 in db.Almacen on tra.AlmacenDestinoId equals al2.AlmacenId
                           join us in db.Usuario on tra.UsuarioId equals us.UsuarioId
                           where tra.TransferenciaId == pTransferenciaId
                           select new EntradaSalida
                           {
                               TransferenciaId= tra.TransferenciaId,
                               AlmacenOrigen = al.Denominacion,
                               AlmacenDestino = al2.Denominacion,
                               UsuarioId = us.NombreUsuario,
                               Fecha = tra.Fecha,
                               Estado = tra.Estado
                               
                           };
                return qry2.FirstOrDefault();
            }

        }

        //public static List<usp_ListarDetalleTransferencia_Result> ListarDetalleTransferencia(int pTransferenciaId)
        //{
        //    using (var db = new VENDIXEntities())
        //    {
        //        return db.usp_ListarDetalleTransferencia(pTransferenciaId).ToList();
        //    }
        //}


        public static bool Desconfirmar(int pTransferenciaId)
        {
            //using (var db = new VENDIXEntities())
            //{
            //    return db.usp_Movimiento_Upd(1, pMovimientoId, null, null, null).ToList()[0];
            //}

            string qry = "UPDATE ALMACEN.Transferencia"+
                "  SET Estado = 'P' WHERE TransferenciaId ="+pTransferenciaId ;
            return EjecutarSql(qry);

        }

        public static string TransferirSeries(int pTransferenciaId)
        {

            
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        var t = db.Transferencia.Find(pTransferenciaId);
                        t.Estado = "C";
                        Actualizar(db,t);

                        var series = TransferenciaSerieBL.Listar(x=>x.TransferenciaId== pTransferenciaId,null,"SerieArticulo");

                        foreach (var item in series)
                        {
                            item.SerieArticulo.EstadoId = 6; // estado trsnsfrido
                            SerieArticuloBL.Actualizar(item.SerieArticulo);
                        }
                    }
                    scope.Complete();
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return ex.Message;
                }
            }

            //string qry = "UPDATE ALMACEN.Transferencia" +
            //   "  SET Estado = 'C' WHERE TransferenciaId =" + pTransferenciaId;

            //String qry2 = " UPDATE ALMACEN.SerieArticulo SET EstadoId = '4'" +
            //    " FROM ALMACEN.TransferenciaSerie INNER JOIN ALMACEN.SerieArticulo" +
            //    " ON ALMACEN.TransferenciaSerie.SerieArticuloId = ALMACEN.SerieArticulo.SerieArticuloId" +
            //    " WHERE TransferenciaId = " + pTransferenciaId;

            //return EjecutarSql(qry);
        }





    }

    //public class EntradaDetalle
    //{
    //    public int TransferenciaId { get; set; }
    //    public int SerieArticuloId { get; set; }
    //    public string NumeroSerie { get; set; }
    //    public string Articulo { get; set; }
        
    //}


}
