using ITB.VENDIX.DA;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ITB.VENDIX.BL
{
    public class DashboardBL
    {
        public static usp_DashboardAdminResumen_Result ObtenerAdminResumen(
            DateTime? fechaCorte = null)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_DashboardAdminResumen(fechaCorte)
                         .FirstOrDefault();
            }
        }

        public static List<usp_DashboardAdminAnalistas_Result> ObtenerAdminAnalistas(
            DateTime? fechaCorte = null)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_DashboardAdminAnalistas(fechaCorte)
                         .ToList();
            }
        }

        public static List<usp_DashboardAdminFlujoCaja_Result> ObtenerAdminFlujoCaja(
            DateTime? fechaCorte = null)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_DashboardAdminFlujoCaja(fechaCorte)
                         .ToList();
            }
        }

        public static List<usp_DashboardAdminHistorico_Result> ObtenerAdminHistorico(
            DateTime? fechaCorte = null,
            int dias = 30)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_DashboardAdminHistorico(fechaCorte, dias)
                         .ToList();
            }
        }

        public static List<usp_DashboardAdminHistoricoMensual_Result>
            ObtenerAdminHistoricoMensual(
                DateTime? fechaCorte = null,
                int meses = 12)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_DashboardAdminHistoricoMensual(fechaCorte, meses)
                         .ToList();
            }
        }

        public static usp_DashboardGestor_Result ObtenerDashboard()
        {
            int usuarioId = VendixGlobal<int>.Obtener("UsuarioId");
            int oficinaId = VendixGlobal<int>.Obtener("OficinaId");

            using (var db = new VENDIXEntities())
            {
                return db.usp_DashboardGestor(
                            usuarioId,
                            oficinaId,
                            (DateTime?)null)
                         .FirstOrDefault();
            }
        }

        public static List<usp_DashboardProductividad_Result> ObtenerProductividad()
        {
            int usuarioId = VendixGlobal<int>.Obtener("UsuarioId");
            int oficinaId = VendixGlobal<int>.Obtener("OficinaId");

            using (var db = new VENDIXEntities())
            {
                return db.usp_DashboardProductividad(usuarioId, oficinaId)
                         .ToList();
            }
        }

        public static List<usp_DashboardGestorClientesMora_Result>
            ObtenerClientesMora(string tipo = "TODOS")
        {
            int usuarioId = VendixGlobal<int>.Obtener("UsuarioId");
            int oficinaId = VendixGlobal<int>.Obtener("OficinaId");

            string tipoNormalizado = string.IsNullOrWhiteSpace(tipo)
                ? "TODOS"
                : tipo.Trim().ToUpperInvariant();

            using (var db = new VENDIXEntities())
            {
                return db.usp_DashboardGestorClientesMora(
                            usuarioId,
                            oficinaId,
                            tipoNormalizado,
                            (DateTime?)null)
                         .ToList();
            }
        }

        /* Métodos legacy conservados hasta retirar sus Function Imports. */
        public static List<usp_DashboardRanking_Result> ObtenerRanking()
        {
            int usuarioId = VendixGlobal<int>.Obtener("UsuarioId");
            int oficinaId = VendixGlobal<int>.Obtener("OficinaId");

            using (var db = new VENDIXEntities())
            {
                return db.usp_DashboardRanking(usuarioId, oficinaId, null)
                         .ToList();
            }
        }

        public static List<usp_DashboardTopAnterior_Result> ObtenerTopAnterior()
        {
            int oficinaId = VendixGlobal<int>.Obtener("OficinaId");

            using (var db = new VENDIXEntities())
            {
                return db.usp_DashboardTopAnterior(oficinaId)
                         .ToList();
            }
        }
    }
}
