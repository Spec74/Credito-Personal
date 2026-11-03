
/*
  Misma “ahora Perú” que dbo.ufnFecha (caja, pagos, tickets).
  SELECT CREDITO.ufn_FechaGerencial()
*/
CREATE OR ALTER FUNCTION CREDITO.ufn_FechaGerencial()
RETURNS DATETIME2(0)
AS
BEGIN
    RETURN CONVERT(DATETIME2(0), dbo.ufnFecha());
END
