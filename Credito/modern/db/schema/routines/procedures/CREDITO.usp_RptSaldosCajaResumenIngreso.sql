--SELECT * FROM CREDITO.CajaDiario WHERE IndCierre=0
--Exec CREDITO.usp_RptSaldosCajaResumenIngreso 19036
CREATE PROC [CREDITO].[usp_RptSaldosCajaResumenIngreso]
@CajaDiarioId INT = 0,
@OficinaId INT = 1
AS

SELECT 'RESUMEN CAJA DIARIO: ' + dbo.ufnResumenCuentaCajaDiario(@CajaDiarioId) 

--DECLARE @SaldoInicial DECIMAL(15,2) = (SELECT SaldoInicial FROM CREDITO.CajaDiario WHERE CajaDiarioId=@CajaDiarioId)

--;WITH SALDOINICIAL AS(	
--	SELECT ItemId 'TipoPagoId',Denominacion, IIF(ItemId=1,@SaldoInicial,0) 'Importe'
--	FROM MAESTRO.ValorTabla 
--	WHERE TablaId=13 AND ItemId>0
--),CUENTAS AS (
--	SELECT TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.ImportePago,-MC.ImportePago) ) 'Importe'
--	FROM CREDITO.MovimientoCaja MC  
--	WHERE MC.CajaDiarioId=@CajaDiarioId 
--	AND MC.Estado=1	
--	GROUP BY MC.TipoPagoId	
--), SALDOS AS(
--	SELECT S.TipoPagoId, S.Denominacion, S.Importe + ISNULL(c.Importe,0) 'Saldo'
--	FROM SALDOINICIAL S
--	LEFT JOIN CUENTAS C ON  c.TipoPagoId= S.TipoPagoId
--)
--SELECT 'RESUMEN CUENTA: ' + STRING_AGG(Denominacion + ' = ' + CAST (Saldo AS VARCHAR(15)),'  ') 'Total'
--FROM SALDOS
--WHERE Saldo>0


