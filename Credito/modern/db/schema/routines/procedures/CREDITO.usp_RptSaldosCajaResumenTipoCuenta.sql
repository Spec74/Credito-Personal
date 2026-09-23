
--Exec CREDITO.usp_RptSaldosCajaResumenTipoCuenta 
CREATE PROC [CREDITO].[usp_RptSaldosCajaResumenTipoCuenta]
@OficinaId INT = 1
AS

DECLARE @SaldoInicial DECIMAL(15,2) = ISNULL((SELECT SUM(SaldoInicial) FROM CREDITO.CajaDiario WHERE TransBoveda=0),0)
 

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
	--ORDER BY s.TipoPagoId
)
SELECT 'RESUMEN CUENTA: ' + STRING_AGG(Denominacion + ' = ' + CAST (Saldo AS VARCHAR(15)),'  ') 'Total'
FROM SALDOS
WHERE Saldo>0





