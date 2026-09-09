


/*
SELECT dbo.ufnCalificar(10,'B')
*/
CREATE FUNCTION [dbo].[ufnCalificar]
    (
      @DiasAtrazo INT ,
      @CalificacionAnterior CHAR(1) 
    )
RETURNS CHAR(1)
AS 
    BEGIN
		DECLARE @Calificacion CHAR(1 ) =	CASE 
												WHEN @DiasAtrazo >= 0 AND @DiasAtrazo<=5 THEN 'A'
												WHEN @DiasAtrazo > 5 AND @DiasAtrazo<=8 THEN 'B'
												WHEN @DiasAtrazo > 8 AND @DiasAtrazo<=12 THEN 'B'
												WHEN @DiasAtrazo > 12 THEN 'C'
											END 
        
		IF @CalificacionAnterior='D' OR @Calificacion='D'
		BEGIN
			RETURN 'D'
		END
		IF @CalificacionAnterior='C' OR @Calificacion='C'
		BEGIN
			RETURN 'C'
		END        
		IF @CalificacionAnterior='B' OR @Calificacion='B'
		BEGIN
			RETURN 'B'
		END

        RETURN 'A'
    END
