-- T-C2: transición CREDITO.Prenda.Estado EN CUSTODIA → DEVUELTO.
-- No crea tablas ni columnas. Fecha/usuario = FechaMod + UsuarioModId (ya existen).
-- No se libera garantía de un crédito DES (el cliente aún debe).
-- Sede = OficinaId del crédito (JWT).

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC [CREDITO].[usp_DevolverPrenda]
    @PrendaId BIGINT,
    @UsuarioId INT,
    @OficinaId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @PrendaId IS NULL OR @PrendaId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
    BEGIN
        RAISERROR(N'Datos de devolución incompletos.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.Prenda AS p
        INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
        WHERE p.PrendaId = @PrendaId
          AND c.OficinaId = @OficinaId
          AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
    )
    BEGIN
        RAISERROR(N'Bien no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Prenda
        WHERE PrendaId = @PrendaId
          AND Estado = N'DEVUELTO'
    )
    BEGIN
        RAISERROR(N'El bien ya fue devuelto.', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Prenda
        WHERE PrendaId = @PrendaId
          AND Estado <> N'EN CUSTODIA'
    )
    BEGIN
        RAISERROR(N'Solo se devuelven bienes en custodia.', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Prenda AS p
        INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
        WHERE p.PrendaId = @PrendaId
          AND c.Estado = N'DES'
    )
    BEGIN
        RAISERROR(N'No se puede devolver mientras el crédito está desembolsado.', 16, 1);
        RETURN;
    END;

    UPDATE CREDITO.Prenda
    SET Estado = N'DEVUELTO',
        UsuarioModId = @UsuarioId,
        FechaMod = dbo.ufnFecha()
    WHERE PrendaId = @PrendaId
      AND Estado = N'EN CUSTODIA';

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'No se actualizó el bien.', 16, 1);
        RETURN;
    END;

    SELECT
        p.PrendaId,
        p.CreditoId,
        p.Estado,
        p.FechaMod
    FROM CREDITO.Prenda AS p
    WHERE p.PrendaId = @PrendaId;
END
GO
