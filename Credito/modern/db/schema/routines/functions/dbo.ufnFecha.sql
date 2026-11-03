
/*
  Hora civil de Perú (America/Lima / SA Pacific Standard Time).
  No depende del huso del servidor SQL (Azure UTC, on-prem, etc.).
  SELECT dbo.ufnFecha()
*/
CREATE OR ALTER FUNCTION [dbo].[ufnFecha]()
RETURNS DATETIME
AS
BEGIN
    RETURN CONVERT(
        DATETIME,
        (SYSUTCDATETIME() AT TIME ZONE N'UTC') AT TIME ZONE N'SA Pacific Standard Time'
    );
END
