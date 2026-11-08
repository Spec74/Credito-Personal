
CREATE OR ALTER PROC [CREDITO].[usp_ResumenCuentaBoveda]
@BovedaId INT
AS

DECLARE @SaldoInicial DECIMAL(15,2) = (SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId=@BovedaId)

IF @SaldoInicial > 0
BEGIN
  IF NOT EXISTS (SELECT BovedaCuentaId FROM CREDITO.BovedaCuenta WHERE BovedaId=@BovedaId)
  BEGIN
     INSERT CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
	 VALUES (@BovedaId, 1, @SaldoInicial)
  END
END

;WITH CUENTAS AS (
	SELECT MC.BovedaId, TipoPagoId, SUM(IIF(MC.IndEntrada=1, MC.Importe, -MC.Importe)) AS Importe
	FROM CREDITO.BovedaMov MC
	WHERE MC.BovedaId = @BovedaId
	  AND MC.Estado = 1
	GROUP BY MC.BovedaId, MC.TipoPagoId
),
SALDOS AS (
	SELECT
		vt.ItemId AS TipoPagoId,
		vt.Denominacion,
		CAST(ISNULL(BC.SaldoInicial, 0) + ISNULL(c.Importe, 0) AS DECIMAL(16, 2)) AS Saldo
	FROM MAESTRO.ValorTabla vt
	LEFT JOIN CUENTAS C ON vt.ItemId = c.TipoPagoId
	LEFT JOIN CREDITO.BovedaCuenta BC
		ON BC.BovedaId = @BovedaId AND BC.TipoPagoId = vt.ItemId
	WHERE vt.TablaId = 13
	  AND vt.ItemId > 0
	  AND vt.ItemId NOT BETWEEN 6 AND 8
	  AND ISNULL(vt.DesCorta, N'') <> N'OBSOLETO'
)
SELECT
	'RESUMEN BOVEDA: ' + STRING_AGG(Denominacion + ' = ' + CAST(Saldo AS VARCHAR(20)), '  ')
		WITHIN GROUP (ORDER BY TipoPagoId) AS Total
FROM SALDOS
WHERE Saldo <> 0
