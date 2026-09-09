
/*
SELECT dbo.ufnCalcularMora(7,5)
*/

CREATE FUNCTION [dbo].[ufnCalcularMora] ( @Importe DECIMAL(16,4) , @DiasAtrazo INT, @DiasGracia INT)
RETURNS DECIMAL(16,2)
AS 
    BEGIN
	    IF @DiasAtrazo <=0
			RETURN 0
        IF @DiasAtrazo <=@DiasGracia
			RETURN 0
        

		RETURN @DiasAtrazo * (@Importe * 0.01)
    END
