using ITB.VENDIX.DA;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using VendixWeb.Models.CierreGerencial;

namespace VendixWeb.Services.CierreGerencial
{
    public interface ICierreGerencialDataService
    {
        IList<MetaGerencialDefinitivaDto> ListarMetas(DateTime periodo);

        IList<AvanceMetaGerencialDto> ObtenerAvance(
            DateTime periodo,
            int oficinaId);

        void GuardarMetas(
            DateTime periodo,
            IList<MetaGerencialDefinitivaDto> metas,
            int usuarioRegistroId);
    }

    public sealed class CierreGerencialDataService
        : ICierreGerencialDataService
    {
        public IList<MetaGerencialDefinitivaDto> ListarMetas(DateTime periodo)
        {
            var periodoNormalizado = PrimerDia(periodo);

            using (var db = new VENDIXEntities())
            {
                db.Database.CommandTimeout = 180;

                return db.Database
                    .SqlQuery<MetaGerencialDefinitivaDto>(
                        "EXEC CREDITO.usp_ListarMetasGerencialesDefinitivas " +
                        "@Periodo",
                        new SqlParameter("@Periodo", periodoNormalizado))
                    .ToList();
            }
        }

        public IList<AvanceMetaGerencialDto> ObtenerAvance(
            DateTime periodo,
            int oficinaId)
        {
            if (oficinaId <= 0)
                throw new ArgumentException(
                    "La oficina indicada no es valida.",
                    "oficinaId");

            var periodoNormalizado = PrimerDia(periodo);

            using (var db = new VENDIXEntities())
            {
                db.Database.CommandTimeout = 180;

                return db.Database
                    .SqlQuery<AvanceMetaGerencialDto>(
                        "EXEC CREDITO.usp_ObtenerAvanceMetasGerenciales " +
                        "@Periodo, @OficinaId",
                        new SqlParameter("@Periodo", periodoNormalizado),
                        new SqlParameter("@OficinaId", oficinaId))
                    .ToList();
            }
        }

        public void GuardarMetas(
            DateTime periodo,
            IList<MetaGerencialDefinitivaDto> metas,
            int usuarioRegistroId)
        {
            if (metas == null || metas.Count == 0)
                throw new ArgumentException(
                    "Debe enviar al menos una meta.",
                    "metas");

            if (usuarioRegistroId <= 0)
                throw new ArgumentException(
                    "La sesion del usuario ha expirado.",
                    "usuarioRegistroId");

            var periodoNormalizado = PrimerDia(periodo);

            if (metas.Select(x => x.UsuarioId).Distinct().Count() != metas.Count)
                throw new ArgumentException(
                    "La solicitud contiene analistas duplicados.",
                    "metas");

            using (var db = new VENDIXEntities())
            using (var transaccion = db.Database.BeginTransaction())
            {
                db.Database.CommandTimeout = 180;

                try
                {
                    foreach (var meta in metas)
                    {
                        ValidarMeta(meta);

                        db.Database.ExecuteSqlCommand(
                            @"EXEC CREDITO.usp_GuardarMetaGerencialDefinitiva
                                @Periodo,
                                @UsuarioId,
                                @MetaCapitalCierre,
                                @MetaClientesActivosCierre,
                                @MetaVencidosMaximoCierre,
                                @MetaRecuperacionVencidosMes,
                                @UsuarioRegistroId",
                            new SqlParameter("@Periodo", periodoNormalizado),
                            new SqlParameter("@UsuarioId", meta.UsuarioId),
                            DecimalNullable(
                                "@MetaCapitalCierre",
                                meta.MetaCapitalCierre),
                            EnteroNullable(
                                "@MetaClientesActivosCierre",
                                meta.MetaClientesActivosCierre),
                            DecimalNullable(
                                "@MetaVencidosMaximoCierre",
                                meta.MetaVencidosMaximoCierre),
                            DecimalNullable(
                                "@MetaRecuperacionVencidosMes",
                                meta.MetaRecuperacionVencidosMes),
                            new SqlParameter(
                                "@UsuarioRegistroId",
                                usuarioRegistroId));
                    }

                    transaccion.Commit();
                }
                catch
                {
                    transaccion.Rollback();
                    throw;
                }
            }
        }

        private static DateTime PrimerDia(DateTime periodo)
        {
            return new DateTime(periodo.Year, periodo.Month, 1);
        }

        private static SqlParameter DecimalNullable(
            string nombre,
            decimal? valor)
        {
            var parametro = new SqlParameter(nombre, SqlDbType.Decimal)
            {
                Precision = 18,
                Scale = 2,
                Value = valor.HasValue
                    ? (object)valor.Value
                    : DBNull.Value
            };

            return parametro;
        }

        private static SqlParameter EnteroNullable(
            string nombre,
            int? valor)
        {
            return new SqlParameter(nombre, SqlDbType.Int)
            {
                Value = valor.HasValue
                    ? (object)valor.Value
                    : DBNull.Value
            };
        }

        private static void ValidarMeta(MetaGerencialDefinitivaDto meta)
        {
            if (meta == null || meta.UsuarioId <= 0)
                throw new ArgumentException(
                    "Existe una meta sin analista valido.",
                    "metas");

            var tipo = (meta.TipoCartera ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            if (tipo != "PRODUCTIVA" && tipo != "ESPECIAL")
                throw new ArgumentException(
                    "Existe una cartera con tipo no valido.",
                    "metas");

            if (!meta.MetaVencidosMaximoCierre.HasValue ||
                !meta.MetaRecuperacionVencidosMes.HasValue)
                throw new ArgumentException(
                    "Las dos metas de vencidos son obligatorias.",
                    "metas");

            if (tipo == "PRODUCTIVA" &&
                (!meta.MetaCapitalCierre.HasValue ||
                 !meta.MetaClientesActivosCierre.HasValue))
                throw new ArgumentException(
                    "Capital y clientes son obligatorios para las carteras productivas.",
                    "metas");

            if ((meta.MetaCapitalCierre.HasValue &&
                 meta.MetaCapitalCierre.Value < 0) ||
                (meta.MetaClientesActivosCierre.HasValue &&
                 meta.MetaClientesActivosCierre.Value < 0) ||
                meta.MetaVencidosMaximoCierre.Value < 0 ||
                meta.MetaRecuperacionVencidosMes.Value < 0)
                throw new ArgumentException(
                    "Las metas no pueden contener valores negativos.",
                    "metas");
        }
    }
}
