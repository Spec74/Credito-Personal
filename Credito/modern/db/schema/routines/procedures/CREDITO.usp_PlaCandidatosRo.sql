CREATE OR ALTER PROC [CREDITO].[usp_PlaCandidatosRo]
    @OficinaId int,
    @Anio int,
    @Mes int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(NULL AS int) AS CreditoId WHERE 1 = 0;
END
