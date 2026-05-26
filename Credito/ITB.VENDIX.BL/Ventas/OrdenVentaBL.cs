using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Objects;
using System.Linq;
using System.Transactions;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class OrdenVentaBL : Repositorio<OrdenVenta>
    {
        public static List<OrdenVentaBuscar> LstOrdenesVentaJGrid(GridDataRequest request, ref int pTotalItems)
        {

            var sClave = request.DataFilters()["Buscar"];
            var sfiltro = bool.Parse(request.DataFilters()["Entregado"]) ? "Estado==\"ENT\" || Estado==\"ANU\"" : "Estado==\"PEN\" || Estado==\"ENV\"";

            using (var db = new VENDIXEntities())
            {
                IQueryable<OrdenVentaBuscar> query =
                    from ov in db.OrdenVenta
                    join c in db.Credito on ov.OrdenVentaId equals c.OrdenVentaId into gj
                    from subpet in gj.DefaultIfEmpty()
                    select new OrdenVentaBuscar
                    {
                        OrdenVentaId = ov.OrdenVentaId,
                        FechaReg = ov.FechaReg,
                        Cliente = ov.Persona.NombreCompleto,
                        TotalNeto = ov.TotalNeto,
                        TotalDescuento = ov.TotalDescuento,
                        TipoVenta = ov.TipoVenta,
                        Estado = ov.Estado,
                        Tags = ov.OrdenVentaId.ToString() + " " + ov.Persona.NombreCompleto,
                        EstadoCredito = (subpet == null ? String.Empty : subpet.Estado)
                    };
                query = query.Where(sfiltro);

                if (sClave != string.Empty)
                {
                    DateTime fecha;
                    query = DateTime.TryParse(sClave, out fecha)
                        ? query.Where(x => EntityFunctions.TruncateTime(x.FechaReg) == fecha.Date)
                        : query.Where("Tags.Contains(\"" + sClave + "\")");
                }

                pTotalItems = query.Count();
                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }

        public static int RealizarPedido(int pClienteId, List<Pedido> pPedidos)
        {

            using (var scope = new TransactionScope())
            {
                try
                {
                    var cabecera = new OrdenVenta
                    {
                        OficinaId = VendixGlobal.GetOficinaId(),
                        Subtotal = 0,
                        TotalNeto = 0,
                        TotalImpuesto = 0,
                        TotalDescuento = 0,
                        Estado = "ENV",
                        UsuarioRegId = VendixGlobal.GetUsuarioId(),
                        FechaReg = VendixGlobal.GetFecha(),
                        PersonaId = pClienteId,
                        TipoVenta = "CON"
                    };
                    Guardar(cabecera);

                    var detalle = new List<OrdenVentaDet>();
                    OrdenVentaDet item;
                    decimal tNeto = 0;
                    decimal tDescuento = 0;

                    foreach (var i in pPedidos)
                    {
                        item = new OrdenVentaDet();
                        var art = ArticuloBL.Obtener(x => x.ArticuloId == i.ArticuloId, "ListaPrecio");
                        var precio = art.ListaPrecio.First().Monto.Value;
                        
                        item.OrdenVentaId = cabecera.OrdenVentaId;
                        item.ArticuloId = art.ArticuloId;
                        item.Cantidad = i.Cantidad;
                        item.Descripcion = art.Denominacion;
                        item.ValorVenta = precio;
                        item.Descuento = i.Descuento;
                        item.Subtotal = i.Cantidad * (precio - i.Descuento);
                        item.Estado = true;

                        var lstSerie = SerieArticuloBL
                            .Listar(x => x.ArticuloId == i.ArticuloId && x.EstadoId == Constante.SerieArticulo.EN_ALMACEN, x => x.OrderBy(y => y.SerieArticuloId))
                            .Take(i.Cantidad);
                        foreach (var l in lstSerie)
                        {
                            item.OrdenVentaDetSerie.Add(new OrdenVentaDetSerie { SerieArticuloId = l.SerieArticuloId });
                            //SerieArticuloBL.ActualizarParcial(
                            //    new SerieArticulo { SerieArticuloId = l.SerieArticuloId, EstadoId = Constante.SerieArticulo.PREVENTA },
                            //    x => x.EstadoId);
                        }
                       
                        detalle.Add(item);

                        tNeto += item.Subtotal;
                        tDescuento += item.Descuento;
                    }
                    OrdenVentaDetBL.Guardar(detalle);

                    cabecera.TotalNeto = tNeto;
                    cabecera.TotalDescuento = tDescuento;
                    cabecera.Subtotal = Math.Round(tNeto / (1 + Constante.IGV), 2);
                    cabecera.TotalImpuesto = Math.Round(tNeto - cabecera.Subtotal, 2);
                    ActualizarParcial(cabecera, x => x.TotalNeto, x => x.TotalDescuento,
                        x => x.Subtotal, x => x.TotalImpuesto);


                    int? retid;
                    using (var db = new VENDIXEntities())
                    {
                        retid = db.usp_PagarCuentaxCobrar(cabecera.OrdenVentaId, 0, VendixGlobal.GetCajaDiarioId(),
                                                      VendixGlobal.GetUsuarioId()).ToList()[0];
                    }

                    scope.Complete();
                    return cabecera.OrdenVentaId;
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    throw new Exception(ex.InnerException.Message);
                }
            }
        }


        public static bool EnviarOrdenVentaCredito(int pOrdenVentaId)
        {
            var orden = Obtener(pOrdenVentaId);
            var glosa = string.Empty;
            decimal inicial = orden.TotalNeto * (decimal)0.15;
            decimal montocredito = orden.TotalNeto - inicial;
            int productoId = 1;
            var gastosadm = GastosAdmBL.CalcularGastosAdm(montocredito,true);
            var lstdes =
                OrdenVentaDetBL.Listar(x => x.Estado && x.OrdenVentaId == orden.OrdenVentaId).Select(x => x.Descripcion)
                    .ToList();
            for (int i = 0; i < lstdes.Count; i++)
            {
                glosa += lstdes[i];
                if (i != lstdes.Count - 1)
                    glosa += ", " + Environment.NewLine;
            }
            var oCredito = new Credito
            {
                OficinaId = VendixGlobal.GetOficinaId(),
                PersonaId = orden.PersonaId,
                Descripcion = glosa,
                MontoProducto = orden.TotalNeto,
                MontoInicial = inicial,
                MontoCredito = montocredito,
                ProductoId = productoId,
                MontoGastosAdm = gastosadm,
                Estado = "CRE",
                FormaPago = "D",
                NumeroCuotas = 26,
                Interes = 7,
                FechaPrimerPago = VendixGlobal.GetFecha(),
                FechaVencimiento = VendixGlobal.GetFecha(),
                FechaReg = VendixGlobal.GetFecha(),
                UsuarioRegId = VendixGlobal.GetUsuarioId(),
                OrdenVentaId = pOrdenVentaId,
                TipoGastoAdm = "CUO",
                TipoCuota = "M"
            };

            using (var scope = new TransactionScope())
            {
                try
                {
                    CreditoBL.Crear(oCredito);
                    orden.Estado = "ENV";
                    orden.TipoVenta = "CRE";                    
                    Actualizar(orden);

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    throw new Exception(ex.InnerException.Message);
                }
            }
            return true;
        }

        public static bool EnviarOrdenVentaContado(int pOrdenVentaId)
        {
            var orden = Obtener(pOrdenVentaId);
            orden.Estado = "ENV";
            orden.TipoVenta = "CON";
            Actualizar(orden);

            //var cxc = new CuentaxCobrar()
            //              {
            //                  PersonaId = orden.PersonaId,
            //                  Operacion = "CON",
            //                  Origen = "ORDEN " + pOrdenVentaId.ToString(),
            //                  Monto = orden.TotalNeto,
            //                  Estado = "PEN",
            //                  FechaReg = VendixGlobal.GetFecha(),
            //                  UsuarioRegId = VendixGlobal.GetUsuarioId()
            //              };

            //using (var scope = new TransactionScope())
            //{
            //    try
            //    {
            //        //CuentaxCobrarBL.Crear(cxc);
            //        //orden.CuentaxCobrarId = cxc.CuentaxCobrarId;
            //        orden.Estado = "ENV";
            //        orden.IndContado = true;
            //        orden.IndCredito = false;
            //        Actualizar(orden);

            //        scope.Complete();
            //    }
            //    catch (Exception ex)
            //    {
            //        scope.Dispose();
            //        throw new Exception(ex.InnerException.Message);
            //    }
            //}
            return true;
        }

        public static int EliminarOrdenVenta(int pOrdenVentaId)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    int ret;
                    using (var db = new VENDIXEntities())
                    {
                        ret = db.usp_OrdenVenta_Del(pOrdenVentaId, 0);
                    }

                    scope.Complete();
                    return ret;
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    throw new Exception(ex.InnerException.Message);
                }
            }
        }

        public class OrdenVentaBuscar : OrdenVenta
        {
            public string Cliente { get; set; }
            public string Tags { get; set; }
            public string EstadoCredito { get; set; }
        }
        public class Pedido
        {
            public int ArticuloId { get; set; }
            public int Cantidad { get; set; }
            public decimal Descuento { get; set; }
        }
    }
}
