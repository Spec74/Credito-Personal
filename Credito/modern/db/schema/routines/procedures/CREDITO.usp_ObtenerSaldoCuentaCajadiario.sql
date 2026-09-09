--EXEC CREDITO.usp_ObtenerSaldoCuentaCajadiario 19006,3
CREATE PROC [CREDITO].[usp_ObtenerSaldoCuentaCajadiario]	
@CajaDiarioId INT,
@TipoPagoId INT=1
AS
DECLARE @SaldoInicial decimal(15,2) = 0, @SaldoCuenta decimal(15,2) = 0
IF	@TipoPagoId=1
BEGIN
    SELECT @SaldoInicial=SaldoInicial FROM CREDITO.CajaDiario WHERE CajaDiarioId=@CajaDiarioId 
END

;WITH CUENTAS AS (
	SELECT TipoPagoId,SUM( IIF(MC.IndEntrada=1, MC.ImportePago,- MC.ImportePago)) 'Saldo'
	FROM CREDITO.MovimientoCaja MC  
	WHERE MC.CajaDiarioId=@CajaDiarioId
	AND MC.Estado=1	AND MC.TipoPagoId=@TipoPagoId
	GROUP BY MC.TipoPagoId	
)
SELECT  @SaldoCuenta = c.Saldo
FROM CUENTAS C 

SELECT @SaldoInicial + ISNULL(@SaldoCuenta,0) 'SaldoCuenta'
