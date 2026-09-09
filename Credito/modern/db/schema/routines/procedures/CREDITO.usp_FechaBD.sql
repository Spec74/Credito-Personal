/*
exec [CREDITO].usp_FechaBD 
*/

CREATE PROC [CREDITO].usp_FechaBD 
AS

SELECT dbo.ufnFecha() 'Fecha'
