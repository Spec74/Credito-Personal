CREATE OR ALTER PROC [CREDITO].[usp_PrendarioActualizarFicha]
    @PrendaId BIGINT,
    @OficinaId INT,
    @UsuarioId INT,
    @Observaciones NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @PrendaId IS NULL OR @PrendaId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de ficha incompletos.', 16, 1);
        RETURN;
    END;

    UPDATE p
    SET p.Observaciones = NULLIF(LTRIM(RTRIM(@Observaciones)), N''),
        p.UsuarioModId = @UsuarioId,
        p.FechaMod = dbo.ufnFecha()
    FROM CREDITO.Prenda AS p
    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
    WHERE p.PrendaId = @PrendaId
      AND c.OficinaId = @OficinaId
      AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2);

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'Bien no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    SELECT p.PrendaId, p.CreditoId, p.Observaciones, p.FotoPath, p.Estado
    FROM CREDITO.Prenda AS p
    WHERE p.PrendaId = @PrendaId;
END
