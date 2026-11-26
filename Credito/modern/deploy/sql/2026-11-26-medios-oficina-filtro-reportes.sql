-- Filtra resumen bóveda / saldos caja por medios activos de la oficina (OficinaMedioPago).
-- Cada sucursal solo ve sus medios configurados en Mantenimiento → Medios de pago.

SET NOCOUNT ON;
GO

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
GO

CREATE OR ALTER PROC [CREDITO].[usp_RptSaldosCajaResumenTipoCuenta]
@OficinaId INT
AS

IF @OficinaId IS NULL OR @OficinaId < 1
BEGIN
    RAISERROR(N'@OficinaId es obligatorio (multisucursal).', 16, 1);
    RETURN;
END

DECLARE @SaldoInicial DECIMAL(15,2) = ISNULL((
    SELECT SUM(CD.SaldoInicial)
    FROM CREDITO.CajaDiario CD
    INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
    WHERE CD.TransBoveda = 0
      AND C.OficinaId = @OficinaId
), 0);

;WITH SALDOINICIAL AS(
	SELECT
		omp.TipoPagoId,
		COALESCE(NULLIF(LTRIM(RTRIM(omp.Nombre)), N''), vt.Denominacion) AS Denominacion,
		IIF(omp.TipoPagoId = 1, @SaldoInicial, 0) AS Importe
	FROM CREDITO.OficinaMedioPago AS omp
	INNER JOIN MAESTRO.ValorTabla AS vt
		ON vt.TablaId = 13 AND vt.ItemId = omp.TipoPagoId
	WHERE omp.OficinaId = @OficinaId
	  AND omp.Activo = 1
	  AND omp.TipoPagoId > 0
),
CUENTAS AS (
	SELECT TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.ImportePago,-MC.ImportePago) ) 'Importe'
	FROM CREDITO.CajaDiario CD
	INNER JOIN	CREDITO.MovimientoCaja MC  ON MC.CajaDiarioId = CD.CajaDiarioId
	INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
	WHERE  CD.TransBoveda=0 AND C.OficinaId=@OficinaId
	AND MC.Estado=1
	GROUP BY MC.TipoPagoId
), SALDOS AS(
	SELECT S.TipoPagoId, S.Denominacion, S.Importe + ISNULL(c.Importe,0) 'Saldo'
	FROM SALDOINICIAL S
	LEFT JOIN CUENTAS C ON  c.TipoPagoId= S.TipoPagoId
)
SELECT 'RESUMEN CUENTA: ' + STRING_AGG(Denominacion + ' = ' + CAST (Saldo AS VARCHAR(15)),'  ') 'Total'
FROM SALDOS
WHERE Saldo>0
GO
