CREATE OR ALTER PROC [CREDITO].[usp_PlaObtenerParametro]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PlaParametroId FROM CREDITO.PlaParametro WHERE PlaParametroId = 1;
END
