CREATE OR ALTER PROC [CREDITO].[usp_PlaRegistrarInusual]
    @OficinaId int,
    @PersonaId int,
    @CreditoId int = NULL,
    @Calificacion varchar(12),
    @Motivo varchar(500),
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(NULL AS int) AS PlaInusualId WHERE 1 = 0;
END
