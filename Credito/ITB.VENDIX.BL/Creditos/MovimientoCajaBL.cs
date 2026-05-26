using System;
using System.Collections.Generic;
using System.Linq;
using ITB.VENDIX.DA;
using System.Data.Objects.SqlClient;

namespace ITB.VENDIX.BL
{
    public class MovimientoCajaBL : Repositorio<MovimientoCaja>
    {
        public static List<decimal> ResumenEntradaSalida(int pCajadiarioId)
        {
            var resumen = new List<decimal>();

            using (var db = new VENDIXEntities())
            {
                if (db.MovimientoCaja.Count(x => x.CajaDiarioId == pCajadiarioId && x.Estado && x.IndEntrada) > 0)
                {
                    resumen.Add(db.MovimientoCaja
                                    .Where(x => x.CajaDiarioId == pCajadiarioId && x.Estado && x.IndEntrada)
                                    .Sum(x => x.ImportePago));
                }
                else
                {
                    resumen.Add(0);
                }

                if (db.MovimientoCaja.Count(x => x.CajaDiarioId == pCajadiarioId && x.Estado && x.IndEntrada == false) >
                    0)
                {
                    resumen.Add(db.MovimientoCaja
                                    .Where(x => x.CajaDiarioId == pCajadiarioId && x.Estado && x.IndEntrada == false)
                                    .Sum(x => x.ImportePago));
                }
                else
                {
                    resumen.Add(0);
                }
            }
            return resumen;
        }

        public static MovCajaCredito RptMovCajaCredito(int pMovimientoCajaId)
        {
            using (var db = new VENDIXEntities())
            {
                var plan = db.PlanPago.Where(x => x.MovimientoCajaId == pMovimientoCajaId && x.Estado=="PAG").ToList();
                var creditoid = plan.First().CreditoId;
                var saldoAnt = plan.OrderBy(x => x.Numero).First().Capital;
                var cuotaspagadas = plan.Max(x => x.Numero);
                var pagodeuda = plan.Sum(x => x.Amortizacion);
                var interes = plan.Sum(x => x.Interes + x.GastosAdm);
                var importelibre = plan.Sum(x => x.PagoLibre);
                var cargosmora = plan.Sum(x => x.ImporteMora + x.Cargo);
                var descuento = plan.Sum(x => x.Descuento);
                var pagocuota = plan.Sum(x => x.PagoCuota.Value);

                var importePagadoCaja = db.MovimientoCaja
                    .Where(x => x.CreditoId == creditoid && x.Estado && x.Operacion == "CUO" &&
                                x.MovimientoCajaId <= pMovimientoCajaId)
                    .Sum(x => x.ImportePago);

                var CreditoTotal = db.PlanPago
                    .Where(x => x.CreditoId == creditoid ).Sum(x => x.Cuota);

                var saldoCapital = CreditoTotal - importePagadoCaja;
                if (saldoCapital < 0) saldoCapital = 0;

                var proxcuota = string.Empty;
                var proxcuotapen = db.PlanPago
                                    .Where(x => x.CreditoId == creditoid && x.Estado == "PEN")
                                    .OrderBy(x => x.Numero).FirstOrDefault();
                if (proxcuotapen != null)
                    proxcuota = proxcuotapen.FechaVencimiento.ToShortDateString();

                var fecha = VendixGlobal.GetFecha();
                var cuotasAtrazadas = db.PlanPago
                    .Count(x => x.CreditoId == creditoid && x.Estado == "PEN" && x.FechaVencimiento < fecha);

                var moraPendiente = db.usp_CalcularMoraPendiente(creditoid).ToList()[0];
                var estadoCredito=db.Credito.Find(creditoid).Estado;
                if (estadoCredito == "PAG")
                    estadoCredito = "CANCELADO";
                else
                    estadoCredito = string.Empty;
                
               var qrycre = from mc in db.MovimientoCaja
                             where mc.MovimientoCajaId == pMovimientoCajaId
                             select new MovCajaCredito
                             {
                                 MovimientoCajaId = mc.MovimientoCajaId,
                                 PersonaId = mc.PersonaId,
                                 Cliente = mc.Persona.NombreCompleto,
                                 User = mc.Usuario.NombreUsuario,
                                 FechaReg = mc.FechaReg,
                                 Oficina = mc.CajaDiario.Caja.Oficina.Denominacion,
                                 Producto = mc.Credito.Producto.Denominacion,
                                 SaldoAnterior = saldoAnt,
                                 PagoDeuda = pagodeuda,
                                 Interes = interes,
                                 MoraCargo = cargosmora,
                                 Descuento = - descuento,
                                 ImporteLibreAnt = - importelibre,
                                 ImporteLibre = mc.ImportePago - pagocuota,
                                 ImportePagado = mc.ImportePago,
                                 SaldoCapital = saldoCapital,
                                 CuotasPagadas = cuotaspagadas + " de " + mc.Credito.NumeroCuotas,
                                 ProximaCuota = proxcuota,
                                 CuotasAtrazadas = cuotasAtrazadas,
                                 MoraTotalPendiente = moraPendiente.Value,
                                 EstadoCredito = estadoCredito,
                                 CreditoTotal = CreditoTotal
                             };
                return qrycre.ToList()[0];
            }
        }

        public static MovCajaBase RptMovCajaInicial(int pMovimientoCajaId)
        {
            using (var db = new VENDIXEntities())
            {
                var credito = db.MovimientoCaja.First(x => x.MovimientoCajaId == pMovimientoCajaId).Credito;
                
                var qrycre = from mc in db.MovimientoCaja
                             where mc.MovimientoCajaId == pMovimientoCajaId
                             select new MovCajaBase
                             {
                                 MovimientoCajaId = mc.MovimientoCajaId,
                                 PersonaId = mc.PersonaId,
                                 Cliente = mc.Persona.NombreCompleto,
                                 User = mc.Usuario.NombreUsuario,
                                 FechaReg = mc.FechaReg,
                                 Oficina = mc.CajaDiario.Caja.Oficina.Denominacion,
                                 Producto = credito.Producto.Denominacion,
                                 ImportePago = mc.ImportePago,
                                 Articulo = credito.IndCondonacion?
                                            "MONTO TOTAL POR PAGAR   " + Math.Round(mc.ImportePago + credito.MontoCondonacion,2).ToString() +
                                            "\n MONTO CONDONADO   " + Math.Round(credito.MontoCondonacion, 2).ToString()
                                            : "",
                                 CreditoId=mc.CreditoId
                                
                             };
                return qrycre.ToList()[0];
            }
        }

        public static MovCajaCredito RptMovCajaLibre(int pMovimientoCajaId)
        {
            using (var db = new VENDIXEntities())
            {
                var credito = db.MovimientoCaja.Find(pMovimientoCajaId).Credito;
                
                var listapagados = db.PlanPago.Where(x => x.CreditoId == credito.CreditoId && x.Estado == "PAG").ToList();
                var cuotaspagadas = 0;
                if (listapagados.Count>0)
                    cuotaspagadas = listapagados.Max(x => x.Numero);

                var CreditoTotal = db.PlanPago
                    .Where(x => x.CreditoId == credito.CreditoId).Sum(x => x.Cuota);

                var importePagadoCaja = db.MovimientoCaja
                    .Where(x => x.CreditoId == credito.CreditoId && x.Estado && x.Operacion == "CUO" &&
                                x.MovimientoCajaId <= pMovimientoCajaId)
                    .Sum(x => x.ImportePago);

                var saldoCapital = CreditoTotal - importePagadoCaja;
                if (saldoCapital < 0) saldoCapital = 0;
                
                var proxcuota = string.Empty;
                var proxcuotapen = db.PlanPago
                                    .Where(x => x.CreditoId == credito.CreditoId && x.Estado == "PEN")
                                    .OrderBy(x => x.Numero).FirstOrDefault();
                if (proxcuotapen != null)
                    proxcuota = proxcuotapen.FechaVencimiento.ToShortDateString();

                
                var fecha = VendixGlobal.GetFecha();
                var cuotasAtrazadas = db.PlanPago
                    .Count(x => x.CreditoId == credito.CreditoId && x.Estado == "PEN" && x.FechaVencimiento < fecha);

                var moraPendiente = db.usp_CalcularMoraPendiente(credito.CreditoId).ToList()[0];
                var estadoCredito = credito.Estado;
                if (estadoCredito == "PAG")
                    estadoCredito = "CANCELADO";
                else
                    estadoCredito = string.Empty;

                var qrycre = from mc in db.MovimientoCaja
                             where mc.MovimientoCajaId == pMovimientoCajaId
                             select new MovCajaCredito
                             {
                                 MovimientoCajaId = mc.MovimientoCajaId,
                                 PersonaId = mc.PersonaId,
                                 Cliente = mc.Persona.NombreCompleto,
                                 User = mc.Usuario.NombreUsuario,
                                 FechaReg = mc.FechaReg,
                                 Oficina = mc.CajaDiario.Caja.Oficina.Denominacion,
                                 Producto = credito.Producto.Denominacion,
                                 ImportePago = mc.ImportePago,
                                 Articulo = mc.Descripcion,
                                 SaldoCapital = saldoCapital,
                                 CuotasPagadas = cuotaspagadas.ToString() + " de " + mc.Credito.NumeroCuotas.ToString(),
                                 ProximaCuota = proxcuota,
                                 CuotasAtrazadas = cuotasAtrazadas,
                                 MoraTotalPendiente = moraPendiente.Value,
                                 EstadoCredito = estadoCredito,
                                 CreditoTotal = CreditoTotal
                             };
                return qrycre.ToList()[0];
            }
        }

        public static MovCajaBase RptMovCajaContado(int pMovimientoCajaId)
        {
            using (var db = new VENDIXEntities())
            {
                var listaart = string.Join(Environment.NewLine,db.MovimientoCaja.Find(pMovimientoCajaId)
                                                       .OrdenVenta.OrdenVentaDet.Select(x => x.Descripcion));
                
                var qrycre = from mc in db.MovimientoCaja
                             where mc.MovimientoCajaId == pMovimientoCajaId
                             select new MovCajaBase
                             {
                                 MovimientoCajaId = mc.MovimientoCajaId,
                                 PersonaId = mc.PersonaId,
                                 Cliente = mc.Persona.NombreCompleto,
                                 User = mc.Usuario.NombreUsuario,
                                 FechaReg = mc.FechaReg,
                                 Oficina = mc.CajaDiario.Caja.Oficina.Denominacion,
                                 Producto = "CREDIEMPRENDE HOGAR - " + mc.Descripcion,
                                 ImportePago = mc.ImportePago,
                                 Articulo = listaart
                             };
                return qrycre.ToList()[0];
            }
        }
        public static MovCajaBase RptMovCajaOtros(int pMovimientoCajaId)
        {
            using (var db = new VENDIXEntities())
            {
                var qrycre = from mc in db.MovimientoCaja
                             where mc.MovimientoCajaId == pMovimientoCajaId
                             select new MovCajaBase
                             {
                                 MovimientoCajaId = mc.MovimientoCajaId,
                                 PersonaId = mc.PersonaId,
                                 Cliente = mc.Persona.NombreCompleto,
                                 User = mc.Usuario.NombreUsuario,
                                 FechaReg = mc.FechaReg,
                                 Oficina = mc.CajaDiario.Caja.Oficina.Denominacion,
                                 Producto = "CAJA DIARIO",
                                 ImportePago = mc.ImportePago,
                                 Articulo = mc.Descripcion,
                                 IndEntrada = mc.IndEntrada
                             };
                return qrycre.ToList()[0];
            }
        }

        public static List<usp_RptMovimientoCajaAnulado_Result> ReporteComprobantesCajaAnulados(DateTime pFechaInicio, DateTime pFechaFin)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptMovimientoCajaAnulado(pFechaInicio, pFechaFin).ToList();
            }
        }
    }

    public class MovCajaBase : MovimientoCaja
    {
        public string Cliente { get; set; }
        public string User { get; set; }
        public string Oficina { get; set; }
        public string Producto { get; set; }
        public string Articulo { get; set; }
    }

    public class MovCajaCredito : MovCajaBase
    {
        public decimal SaldoAnterior { get; set; }
        public decimal PagoDeuda { get; set; }
        public decimal Interes { get; set; }
        public decimal MoraCargo { get; set; }
        public decimal Descuento { get; set; }
        public decimal ImporteLibreAnt { get; set; }
        public decimal ImporteLibre { get; set; }
        public decimal ImportePagado { get; set; }
        public decimal SaldoCapital { get; set; }
        public string CuotasPagadas { get; set; }
        public string ProximaCuota { get; set; }
        public int CuotasAtrazadas { get; set; }
        public decimal MoraTotalPendiente { get; set; }
        public decimal CreditoTotal { get; set; }
        public string EstadoCredito { get; set; }
    }

}
