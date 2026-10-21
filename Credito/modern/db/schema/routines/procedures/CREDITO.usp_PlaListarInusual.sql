CREATE OR ALTER PROC [CREDITO].[usp_PlaListarInusual]
    @OficinaId int,
    @Anio int,
    @Mes int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(NULL AS int) AS PlaInusualId WHERE 1 = 0;
END
