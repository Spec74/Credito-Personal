


/*SELECT dbo.ufnCalcularDiasAtrazo('20140210')*/
CREATE FUNCTION [dbo].[ufnCalcularDiasAtrazo]
    (
      @FechaVencimiento DATE ,
      @FechaHoy DATE 
    )
RETURNS INTEGER
AS 
    BEGIN
    
        DECLARE @dia_sem INT ,
            @domingos INT ,
            @fecha DATE ,
            @diasAtrazo INT= 0
    
        SET @fecha = @FechaVencimiento
        SET @domingos = 0
        WHILE @fecha <= @FechaHoy 
            BEGIN   
                SELECT  @dia_sem = DATEPART(weekday, @fecha) 
                IF @dia_sem = 1 
                    SET @domingos = @domingos + 1    
                SELECT  @fecha = DATEADD(dd, 1, @fecha)     
            END
        SET @diasAtrazo = DATEDIFF(DAY, @FechaVencimiento, @FechaHoy) - @domingos
        IF @diasAtrazo < 0 
            SET @diasAtrazo = 0
        
        RETURN @diasAtrazo
    END
