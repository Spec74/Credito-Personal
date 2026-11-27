using System.Data;
using Dapper;

namespace Credito.Modern.Infrastructure.MediosPago;

/// <summary>
/// Semilla idempotente de medios base por oficina (compartida por listado, validación y cobros).
/// </summary>
internal static class OficinaMedioPagoSeedSql
{
    internal static async Task EnsureSemillaOficinaAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int oficinaId,
        CancellationToken cancellationToken)
    {
        try
        {
            await connection
                .ExecuteAsync(
                    new CommandDefinition(
                        """
                    ;WITH Catalogo AS (
                        SELECT
                            vt.ItemId AS TipoPagoId,
                            LTRIM(RTRIM(vt.Denominacion)) AS Nombre,
                            CASE
                                WHEN vt.ItemId = 1 THEN N'efectivo'
                                WHEN vt.ItemId IN (2, 10, 11) THEN N'yape'
                                WHEN vt.ItemId IN (3, 12, 13) THEN N'interbank'
                                WHEN vt.ItemId = 4 THEN N'bcp'
                                WHEN vt.ItemId = 5 THEN N'banco-nacion'
                                WHEN vt.ItemId = 9 THEN N'plin'
                                ELSE N'otro'
                            END AS MarcaCodigo,
                            CASE
                                WHEN vt.ItemId = 1 THEN 10
                                WHEN vt.ItemId IN (10, 11) THEN 20
                                WHEN vt.ItemId = 2 THEN 22
                                WHEN vt.ItemId IN (12, 13) THEN 30
                                WHEN vt.ItemId = 3 THEN 32
                                WHEN vt.ItemId = 4 THEN 40
                                WHEN vt.ItemId = 5 THEN 50
                                WHEN vt.ItemId = 9 THEN 60
                                ELSE 100 + vt.ItemId
                            END AS Orden,
                            CAST(CASE WHEN vt.ItemId > 1 THEN 1 ELSE 0 END AS bit) AS EsDigital,
                            CAST(CASE WHEN vt.ItemId IN (2, 9, 10, 11) THEN 1 ELSE 0 END AS bit) AS UsaCelular,
                            CAST(CASE WHEN vt.ItemId IN (3, 4, 5, 12, 13) THEN 1 ELSE 0 END AS bit) AS UsaCuenta,
                            CAST(CASE WHEN vt.ItemId IN (2, 9, 10, 11) THEN 1 ELSE 0 END AS bit) AS UsaQr,
                            CASE
                                WHEN vt.ItemId IN (2, 10, 11) THEN N'YAPE'
                                WHEN vt.ItemId IN (3, 12, 13) THEN N'INTERBANK'
                                ELSE NULL
                            END AS FamiliaCodigo,
                            CAST(CASE WHEN vt.ItemId BETWEEN 10 AND 13 THEN 1 ELSE 0 END AS bit) AS SoloCentral
                        FROM MAESTRO.ValorTabla AS vt
                        WHERE vt.TablaId = 13
                          AND vt.ItemId > 0
                          AND vt.ItemId NOT BETWEEN 6 AND 8
                    )
                    INSERT INTO CREDITO.OficinaMedioPago (
                        OficinaId, TipoPagoId, Nombre, MarcaCodigo, Orden, Activo,
                        EsDigital, UsaCelular, UsaCuenta, UsaQr, FamiliaCodigo, SoloCentral
                    )
                    SELECT
                        o.OficinaId,
                        c.TipoPagoId,
                        c.Nombre,
                        c.MarcaCodigo,
                        c.Orden,
                        1,
                        c.EsDigital,
                        c.UsaCelular,
                        c.UsaCuenta,
                        c.UsaQr,
                        c.FamiliaCodigo,
                        c.SoloCentral
                    FROM MAESTRO.Oficina AS o
                    CROSS JOIN Catalogo AS c
                    WHERE o.OficinaId = @OficinaId
                      AND (
                            c.SoloCentral = 0
                            OR ISNULL(o.IndPrincipal, 0) = 1
                          )
                      AND NOT EXISTS (
                            SELECT 1
                            FROM CREDITO.OficinaMedioPago AS m
                            WHERE m.OficinaId = o.OficinaId
                              AND m.TipoPagoId = c.TipoPagoId
                          );
                    """,
                        new { OficinaId = oficinaId },
                        transaction: transaction,
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (
            ex.Message.Contains("OficinaMedioPago", StringComparison.OrdinalIgnoreCase)
            && (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("no existe", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Falta CREDITO.OficinaMedioPago. Ejecute deploy/sql/2026-11-25-oficina-medio-pago.sql antes de configurar medios.",
                ex);
        }
    }
}
