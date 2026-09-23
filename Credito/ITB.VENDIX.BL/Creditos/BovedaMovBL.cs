using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class BovedaMovBL : Repositorio<BovedaMov>
    {
        public static bool IngresoEgresoBovedaCaja(decimal pImporte, string pDescripcion, int pTipoOperacionId, short pTipoCuentaId)
        {
            var pUsuarioRegId = VendixGlobal.GetUsuarioId();
            var oficinaId = VendixGlobal.GetOficinaId();

            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        var bovedaId = db.Boveda.First(x => x.OficinaId == oficinaId && x.IndCierre == false).BovedaId;
                        var oTipoOperacion = db.TipoOperacion.Find(pTipoOperacionId);
                        var personaId = db.Usuario.First(x => x.UsuarioId == pUsuarioRegId).PersonaId;

                        db.BovedaMov.Add(new BovedaMov
                        {
                            BovedaId = bovedaId,
                            CodOperacion = oTipoOperacion.Codigo,
                            Glosa = pDescripcion.ToUpper(),
                            TipoPagoId = pTipoCuentaId,
                            Importe = pImporte,
                            IndEntrada = oTipoOperacion.IndEntrada,
                            Estado = true,
                            CajaDiarioId = 0,
                            UsuarioRegId = pUsuarioRegId,
                            FechaReg = VendixGlobal.GetFecha()
                        });
                        db.SaveChanges();

                        db.usp_ActualizarSaldosBoveda(bovedaId);
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

        public static bool AsignarBovedaTemporal(decimal pImporte, string pDescripcion, int pUsuarioId)
        {
            var pUsuarioRegId = VendixGlobal.GetUsuarioId();
            var oficinaId = VendixGlobal.GetOficinaId();
            var bovedaId = VendixGlobal.GetBovedaId();
            int bovedaTemporalId = 0;
            var bovedatemporal = BovedaBL.Obtener(x => x.OficinaId == oficinaId && x.IndCierre == false && x.IndTemporal);
            pDescripcion = "ASIGNACION TEMPORAL: " + pDescripcion;
            Boveda objtemp = null;
            if (bovedatemporal == null)
            {
                objtemp = new Boveda()
                {
                    OficinaId = oficinaId,
                    SaldoInicial = 0,
                    SaldoFinal = 0,
                    Entradas = 0,
                    Salidas = 0,
                    FechaIniOperacion = VendixGlobal.GetFecha(),
                    FechaFinOperacion = null,
                    IndCierre = false,
                    IndTemporal = true
                };
                BovedaBL.Crear(objtemp);
                bovedaTemporalId = objtemp.BovedaId;
            }
            else
            {
                bovedaTemporalId = bovedatemporal.BovedaId;
            }

            using (var scope = new TransactionScope())
            {
                try
                {
                    string sRol = "ENCARGADO";
                    using (var db = new VENDIXEntities())
                    {
                        var rolEncargado = db.Rol.FirstOrDefault(x => x.Denominacion.Contains(sRol) && x.Estado);
                        if (rolEncargado == null)
                        {
                            rolEncargado = new Rol() { Denominacion = sRol, Estado = true };
                            db.Rol.Add(rolEncargado);
                            db.SaveChanges();
                        }
                        var UsuarioRolEncargado = db.UsuarioRol.FirstOrDefault(x => x.RolId == rolEncargado.RolId
                                                                    && x.UsuarioId == pUsuarioId && x.OficinaId == oficinaId);
                        if (UsuarioRolEncargado == null)
                        {
                            UsuarioRolEncargado = new UsuarioRol() { OficinaId = oficinaId, RolId = rolEncargado.RolId, UsuarioId = pUsuarioId };
                            db.UsuarioRol.Add(UsuarioRolEncargado);
                            db.SaveChanges();
                        }
                        var mnuSaldos = db.Menu.First(x => x.Url.Contains("Saldos")).MenuId;
                        var rolMenuEncargado = db.RolMenu.FirstOrDefault(x => x.RolId == rolEncargado.RolId && x.MenuId == mnuSaldos);
                        if (rolMenuEncargado == null)
                        {
                            rolMenuEncargado = new RolMenu() { RolId = rolEncargado.RolId, MenuId = mnuSaldos };
                            db.RolMenu.Add(rolMenuEncargado);
                            db.SaveChanges();
                        }
                        db.usp_TransferirBoveda(bovedaId, bovedaTemporalId, pDescripcion, pImporte, pUsuarioRegId, 0, 0);
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

        public static bool Tranferir_a_BovedaTemporal(decimal pImporte, string pDescripcion)
        {
            var pUsuarioRegId = VendixGlobal.GetUsuarioId();
            var oficinaId = VendixGlobal.GetOficinaId();
            var bovedaId = VendixGlobal.GetBovedaId();

            var bovedatemporal = BovedaBL.Obtener(x => x.OficinaId == oficinaId && x.IndCierre == false && x.IndTemporal);

            pDescripcion = "TRANS A BOVEDA TEMPORAL: " + pDescripcion;

            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_TransferirBoveda(bovedaId, bovedatemporal.BovedaId, pDescripcion, pImporte, pUsuarioRegId, 0, 0);
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

        public static bool TransferirBovedaCaja(decimal pImporte, string pDescripcion, int pCajaId, short pTipoPagoOrigenId = 1, short pTipoPagoDestinoId = 1)
        {
            var pUsuarioRegId = VendixGlobal.GetUsuarioId();
            var oficinaId = VendixGlobal.GetOficinaId();

            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        var bovedaId = db.Boveda.First(x => x.OficinaId == oficinaId && x.IndCierre == false).BovedaId;
                        var oCajaDiario = db.CajaDiario.First(x => x.CajaId == pCajaId && x.IndCierre == false);
                        var personaId = db.Usuario.First(x => x.UsuarioId == pUsuarioRegId).PersonaId;

                        // 1. MOVIMIENTO DE BOVEDA (Descuenta del Banco de Origen elegido)
                        var objBovMov = new BovedaMov()
                        {
                            BovedaId = bovedaId,
                            CodOperacion = "TRS",
                            Glosa = "TRANS A CAJA: " + pDescripcion.ToUpper(),
                            Importe = pImporte,
                            IndEntrada = false, // Salida de dinero
                            Estado = true,
                            CajaDiarioId = oCajaDiario.CajaDiarioId,
                            UsuarioRegId = pUsuarioRegId,
                            FechaReg = VendixGlobal.GetFecha(),
                            TipoPagoId = pTipoPagoOrigenId // ¡PIEZA FALTANTE CORREGIDA! Ahora la Bóveda sabe qué banco restar
                        };
                        BovedaMovBL.Crear(objBovMov);

                        // 2. MOVIMIENTO DE CAJA ANALISTA (Suma al Efectivo o Destino gracias al Embudo)
                        db.MovimientoCaja.Add(new MovimientoCaja
                        {
                            CajaDiarioId = oCajaDiario.CajaDiarioId,
                            Operacion = "TRE",
                            ImportePago = pImporte,
                            Descripcion = "[MovBoveda:" + objBovMov.MovimientoBovedaId.ToString() + "] " + "TRANS DE BOVEDA: " + pDescripcion.ToUpper(),
                            IndEntrada = true, // Entrada de dinero
                            Estado = true,
                            PersonaId = personaId,
                            TipoPagoId = pTipoPagoDestinoId, // Usa el parámetro de destino (Por defecto 1 = Efectivo)
                            UsuarioRegId = pUsuarioRegId,
                            FechaReg = VendixGlobal.GetFecha()
                        });
                        db.SaveChanges();

                        // Recalcular entradas, salidas y saldos diarios del analista
                        var qry = db.MovimientoCaja.Where(z => z.CajaDiarioId == oCajaDiario.CajaDiarioId && z.Estado).Select(x => new { x.ImportePago, x.IndEntrada });
                        if (qry.Count(x => x.IndEntrada) > 0)
                            oCajaDiario.Entradas = qry.Where(z => z.IndEntrada).Sum(x => x.ImportePago);
                        if (qry.Count(x => x.IndEntrada == false) > 0)
                            oCajaDiario.Salidas = qry.Where(z => z.IndEntrada == false).Sum(x => x.ImportePago);

                        oCajaDiario.SaldoFinal = oCajaDiario.SaldoInicial + oCajaDiario.Entradas - oCajaDiario.Salidas;

                        // Actualizar los saldos en tiempo real de los Cards de la Bóveda en la BD
                        db.usp_ActualizarSaldosBoveda(bovedaId);

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
        public static string TransferirBovedaCajaChica(decimal pImporte, string pDescripcion)
        {
            var pUsuarioRegId = VendixGlobal.GetUsuarioId();
            var oficinaId = VendixGlobal.GetOficinaId();

            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        var bovedaId = db.Boveda.First(x => x.OficinaId == oficinaId && x.IndCierre == false).BovedaId;
                        var oCajaDiario = db.CajaChicaDiario.FirstOrDefault(x => x.IndCierre == false);
                        if (oCajaDiario == null) return "NO EXISTE CAJA CHICA ABIERTA";

                        var personaId = db.Usuario.First(x => x.UsuarioId == pUsuarioRegId).PersonaId;

                        db.BovedaMov.Add(new BovedaMov
                        {
                            BovedaId = bovedaId,
                            CodOperacion = "TRS",
                            Glosa = "TRANS A CAJA CHICA: " + pDescripcion.ToUpper(),
                            Importe = pImporte,
                            IndEntrada = false,
                            Estado = true,
                            CajaDiarioId = oCajaDiario.Id,
                            UsuarioRegId = pUsuarioRegId,
                            FechaReg = VendixGlobal.GetFecha()
                        });

                        db.MovimientoCajaChica.Add(new MovimientoCajaChica
                        {
                            CajaChicaDiarioId = oCajaDiario.Id,
                            Operacion = "TRE",
                            Importe = pImporte,
                            Descripcion = "TRANS DE BOVEDA: " + pDescripcion.ToUpper(),
                            IndEntrada = true,
                            Estado = true,
                            PersonaId = personaId,
                            UsuarioRegId = pUsuarioRegId,
                            FechaReg = VendixGlobal.GetFecha(),
                            IndRendido = false,
                            ImporteRendido = 0
                        });
                        db.SaveChanges();

                        var qry = db.MovimientoCajaChica.Where(z => z.CajaChicaDiarioId == oCajaDiario.Id && z.Estado).Select(x => new { x.Importe, x.IndEntrada });
                        if (qry.Count(x => x.IndEntrada) > 0)
                            oCajaDiario.Entradas = qry.Where(z => z.IndEntrada).Sum(x => x.Importe);
                        if (qry.Count(x => x.IndEntrada == false) > 0)
                            oCajaDiario.Salidas = qry.Where(z => z.IndEntrada == false).Sum(x => x.Importe);

                        oCajaDiario.SaldoFinal = oCajaDiario.SaldoInicial + oCajaDiario.Entradas - oCajaDiario.Salidas;

                        db.usp_ActualizarSaldosBoveda(bovedaId);

                        db.SaveChanges();
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
        }

        public static bool TransferiraOficina(decimal pImporte, string pDescripcion, int pBovedaInicioId, int pBovedaDestinoId, int pUsuarioRegId)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    using (var db = new VENDIXEntities())
                    {
                        db.usp_TransferirBoveda(pBovedaInicioId, pBovedaDestinoId, pDescripcion, pImporte, pUsuarioRegId, 0, 0);
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

        public static bool TransferiraOficina(int pBovedaMovTempId, int flag)
        {
            using (var scope = new TransactionScope())
            {
                try
                {

                    using (var db = new VENDIXEntities())
                    {
                        db.usp_TransferirBoveda(0, 0, "", 0, 0, flag, pBovedaMovTempId);
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

        public static List<Transferencias> ListarTransferencias()
        {
            var bovedaId = VendixGlobal<int>.Obtener("BovedaId");
            List<BovedaMovTemp> lista;
            var listTransferencias = new List<Transferencias>();
            using (var db = new VENDIXEntities())
            {
                lista = db.BovedaMovTemp.Where(x => x.BovedaDestinoId == bovedaId).ToList();
            }

            foreach (var item in lista)
            {
                var oficinaId = BovedaBL.Obtener(z => z.BovedaId == item.BovedaInicioId).OficinaId;
                var personaId = UsuarioBL.Obtener(z => z.UsuarioId == item.UsuarioRegId).PersonaId;

                listTransferencias.Add(new Transferencias
                {
                    TransferenciaId = item.BovedaMovTempId,
                    Monto = item.Importe,
                    From = OficinaBL.Obtener(x => x.OficinaId == oficinaId).Denominacion,
                    Descripcion = item.Glosa,
                    UsuarioReg = PersonaBL.Obtener(x => x.PersonaId == personaId).NombreCompleto,
                });
            }
            return listTransferencias;
        }

        public static bool TransferirEntreBancos(int pBovedaId, short pTipoPagoOrigenId, short pTipoPagoDestinoId, decimal pImporte, string pGlosa, int pUsuarioRegId)
        {
            // 1. El TransactionScope protege toda la operación desde .NET
            using (var scope = new TransactionScope())
            {
                try
                {
                    // 2. Usamos tu DbContext real de la base de datos
                    using (var db = new VENDIXEntities())
                    {
                        // 3. En tu versión de Entity Framework se usa ExecuteSqlCommand
                        db.Database.ExecuteSqlCommand(
                            "EXEC CREDITO.usp_RegistrarTransferenciaBancos @BovedaId={0}, @TipoPagoOrigenId={1}, @TipoPagoDestinoId={2}, @Importe={3}, @Glosa={4}, @UsuarioRegId={5}",
                            pBovedaId, pTipoPagoOrigenId, pTipoPagoDestinoId, pImporte, pGlosa, pUsuarioRegId
                        );
                    }

                    // 4. Si la base de datos no lanzó ningún error, confirmamos la transacción
                    scope.Complete();
                    return true;
                }
                catch (Exception)
                {
                    // 5. Si algo falló, cancelamos todo para que no se descuadre la bóveda
                    scope.Dispose();
                    return false;
                }
            }
        }
    }

    public class Transferencias
    {
        public int TransferenciaId { get; set; }
        public decimal Monto { get; set; }
        public string From { get; set; }
        public string Descripcion { get; set; }
        public string UsuarioReg { get; set; }
    }
}
