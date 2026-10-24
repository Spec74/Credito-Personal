namespace Credito.Modern.Infrastructure.CreditoPlanes;

/// <summary>
/// Σ medios = BovedaCuenta.SI + netos de BovedaMov, misma cuenta que
/// <c>usp_ResumenCuentaBoveda</c>. Si no hay filas en BovedaCuenta, el SI de
/// cabecera se atribuye a efectivo (TipoPagoId = 1), sin INSERT colateral.
/// </summary>
internal static class BovedaCuadreSql
{
    internal const decimal Tolerancia = 0.01m;

    internal const string SumaMedios = """
        SELECT CAST(ISNULL(SUM(
            ISNULL(bc.SaldoInicial, 0)
            + ISNULL(m.Importe, 0)
            + CASE
                WHEN vt.ItemId = 1
                 AND NOT EXISTS (
                    SELECT 1 FROM CREDITO.BovedaCuenta AS x WHERE x.BovedaId = @BovedaId)
                THEN ISNULL((
                    SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId = @BovedaId), 0)
                ELSE 0
              END
        ), 0) AS decimal(16, 2))
        FROM MAESTRO.ValorTabla AS vt
        LEFT JOIN CREDITO.BovedaCuenta AS bc
            ON bc.BovedaId = @BovedaId AND bc.TipoPagoId = vt.ItemId
        LEFT JOIN (
            SELECT TipoPagoId, SUM(IIF(IndEntrada = 1, Importe, -Importe)) AS Importe
            FROM CREDITO.BovedaMov
            WHERE BovedaId = @BovedaId AND Estado = 1
            GROUP BY TipoPagoId
        ) AS m ON m.TipoPagoId = vt.ItemId
        WHERE vt.TablaId = 13 AND vt.ItemId > 0;
        """;
}
