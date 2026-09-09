
/*
SELECT dbo.ufnFecha()
*/
CREATE   FUNCTION [dbo].[ufnFecha]
    (
    )
RETURNS DATETIME
AS 
    BEGIN	

        RETURN DATEADD(HOUR,2,GETDATE()) 
    END
