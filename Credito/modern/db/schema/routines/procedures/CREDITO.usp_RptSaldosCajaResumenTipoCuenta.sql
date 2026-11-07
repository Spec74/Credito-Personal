
--Exec CREDITO.usp_RptSaldosCajaResumenTipoCuenta @OficinaId=1
CREATE OR ALTER PROC [CREDITO].[usp_RptSaldosCajaResumenTipoCuenta]
@OficinaId INT
AS

IF @OficinaId IS NULL OR @OficinaId < 1
BEGIN
    RAISERROR(N'@OficinaId es obligatorio (multisucursal).', 16, 1);
    RETURN;
END

-- Saldo inicial efectivo solo de la oficina (no de toda la red).
DECLARE @SaldoInicial DECIMAL(15,2) = ISNULL((
    SELECT SUM(CD.SaldoInicial)
    FROM CREDITO.CajaDiario CD
    INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
    WHERE CD.TransBoveda = 0
      AND C.OficinaId = @OficinaId
), 0);


;WITH SALDOINICIAL AS(
	SELECT ItemId 'TipoPagoId',Denominacion, IIF(ItemId=1,@SaldoInicial,0) 'Importe'
	FROM MAESTRO.ValorTabla
	WHERE TablaId=13 AND ItemId>0
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
