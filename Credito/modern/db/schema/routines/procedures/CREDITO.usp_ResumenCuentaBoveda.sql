
CREATE OR ALTER PROC [CREDITO].[usp_ResumenCuentaBoveda]
@BovedaId INT
AS

DECLARE @SaldoInicial DECIMAL(15,2) = (SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId=@BovedaId)
DECLARE @OficinaId INT = (SELECT OficinaId FROM CREDITO.Boveda WHERE BovedaId=@BovedaId)

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
		omp.TipoPagoId,
		COALESCE(NULLIF(LTRIM(RTRIM(omp.Nombre)), N''), vt.Denominacion) AS Denominacion,
		CAST(ISNULL(BC.SaldoInicial, 0) + ISNULL(c.Importe, 0) AS DECIMAL(16, 2)) AS Saldo
	FROM CREDITO.OficinaMedioPago AS omp
	INNER JOIN MAESTRO.ValorTabla AS vt
		ON vt.TablaId = 13 AND vt.ItemId = omp.TipoPagoId
	LEFT JOIN CUENTAS C ON omp.TipoPagoId = c.TipoPagoId
	LEFT JOIN CREDITO.BovedaCuenta BC
		ON BC.BovedaId = @BovedaId AND BC.TipoPagoId = omp.TipoPagoId
	WHERE omp.OficinaId = @OficinaId
	  AND omp.Activo = 1
	  AND omp.TipoPagoId > 0
)
SELECT COALESCE((
	SELECT
		N'RESUMEN BOVEDA: ' + STRING_AGG(Denominacion + ' = ' + CAST(Saldo AS VARCHAR(20)), '  ')
			WITHIN GROUP (ORDER BY TipoPagoId)
	FROM SALDOS
	WHERE Saldo <> 0
), N'RESUMEN BOVEDA: ') AS Total
