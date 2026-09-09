/*
SELECT dbo.ufnCalcularImporteCobrado(2023,4,1099)
*/

CREATE FUNCTION [dbo].[ufnCalcularImporteCobrado] ( @Anio INT , @Mes INT, @UsuarioRegId INT)
RETURNS DECIMAL(16,2)
AS 
    BEGIN
	    DECLARE	@FechaInicio DATE = CAST(@Anio AS VARCHAR(4)) + '-' + CAST(@Mes AS VARCHAR(2))  + '-01' 
		DECLARE	@FechaFin DATE = DATEADD(DAY,-1,DATEADD(MONTH,1,@FechaInicio))
		DECLARE @ImporteCobrado DECIMAL(15,2) = 0

		--SELECT mc.*
		SELECT @ImporteCobrado = SUM(MC.ImportePago)
		FROM CREDITO.MovimientoCaja MC
		INNER JOIN CREDITO.Credito C ON C.CreditoId = MC.CreditoId
		WHERE MC.Operacion='CUO' AND MC.Estado=1 AND MC.IndEntrada=1 AND MC.ImportePago>0
		AND CAST(MC.FechaReg AS DATE) BETWEEN @FechaInicio AND @FechaFin 
		AND C.UsuarioRegId = @UsuarioRegId

		RETURN ISNULL(@ImporteCobrado,0)
    END
