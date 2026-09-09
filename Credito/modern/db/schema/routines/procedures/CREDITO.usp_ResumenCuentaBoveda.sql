
--SELECT * FROM CREDITO.BovedaCuenta
--SELECT * FROM CREDITO.Boveda WHERE BovedaId=6733
--SELECT * FROM CREDITO.BovedaMov WHERE BovedaId=6732


--Exec CREDITO.usp_ResumenCuentaBoveda 6737
CREATE PROC [CREDITO].[usp_ResumenCuentaBoveda]
@BovedaId INT 	
AS

DECLARE @SaldoInicial DECIMAL(15,2) = (SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId=@BovedaId)

IF	@SaldoInicial>0
BEGIN
  IF NOT EXISTS(SELECT BovedaCuentaId FROM CREDITO.BovedaCuenta WHERE BovedaId=@BovedaId )
	BEGIN
     INSERT CREDITO.BovedaCuenta (BovedaId, TipoPagoId,SaldoInicial)   
	 VALUES (@BovedaId, 1, @SaldoInicial  )
	END  
END

;WITH CUENTAS AS (
	SELECT MC.BovedaId,TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.Importe,-MC.Importe) ) 'Importe'
	FROM CREDITO.BovedaMov MC  
	WHERE MC.BovedaId=@BovedaId 
	AND MC.Estado=1	
	GROUP BY MC.BovedaId,MC.TipoPagoId	
)
SELECT 'RESUMEN BOVEDA: ' + STRING_AGG(vt.Denominacion + ' = ' + CAST ( ISNULL(BC.SaldoInicial,0) +ISNULL(c.Importe,0)  AS VARCHAR(15)),'  ') 'Total'
FROM MAESTRO.ValorTabla vt
LEFT JOIN CUENTAS C ON  vt.ItemId=c.TipoPagoId
LEFT JOIN CREDITO.BovedaCuenta BC ON BC.BovedaId = @BovedaId and BC.TipoPagoId = vt.ItemId
WHERE vt.TablaId=13 AND vt.ItemId>0
