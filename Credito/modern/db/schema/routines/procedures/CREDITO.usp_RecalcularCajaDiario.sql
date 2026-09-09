

CREATE PROCEDURE [CREDITO].[usp_RecalcularCajaDiario](@CajaDiarioId INT)

AS
DECLARE @Entradas DECIMAL(16,2), @Salidas DECIMAL(16,2)

BEGIN 

	

	SELECT @entradas=SUM(ImportePago) 
	FROM CREDITO.MovimientoCaja
	WHERE CajaDiarioId=@CajaDiarioId AND Estado=1 AND IndEntrada=1

	SELECT @salidas=SUM(ImportePago) 
	FROM CREDITO.MovimientoCaja
	WHERE CajaDiarioId=@CajaDiarioId AND Estado=1 AND IndEntrada=0

	UPDATE CREDITO.CajaDiario 
	SET Entradas=ISNULL(@entradas,0) , Salidas = ISNULL(@salidas,0) , 
		SaldoFinal = ROUND(SaldoInicial + ISNULL(@entradas,0) - ISNULL(@salidas,0),1)
	WHERE CajaDiarioId=@CajaDiarioId
	
END
