
CREATE PROCEDURE [CREDITO].[usp_ActualizarSaldosBoveda](@BovedaId INT)

AS
DECLARE @Entradas DECIMAL(16,2), @Salidas DECIMAL(16,2), @SaldoInicial DECIMAL(16,2)

BEGIN 

	SELECT @Entradas = ISNULL(SUM(Importe),0) FROM credito.bovedamov 
	WHERE BovedaId= @BovedaId AND indEntrada = 1 AND Estado= 1
	
	SELECT @Salidas = ISNULL(SUM(Importe),0)  FROM credito.bovedamov 
	WHERE BovedaId= @BovedaId AND indEntrada = 0 AND Estado= 1

	SET @SaldoInicial = (SELECT SaldoInicial FROM CREDITO.BOVEDA 
						WHERE BovedaId= @BovedaId)
	
	UPDATE CREDITO.BOVEDA SET Entradas = @Entradas, Salidas = @Salidas ,
	SaldoFinal = @SaldoInicial + @Entradas - @Salidas 
	WHERE BovedaId = @BovedaId --AND IndCierre = 0
	
END
