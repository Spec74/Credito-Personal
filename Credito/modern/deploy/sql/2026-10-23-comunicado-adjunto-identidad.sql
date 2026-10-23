-- SCOPE_IDENTITY() es numeric; CAST a int evita fallo de mapeo al devolver AdjuntoId.
-- Idempotente. No cambia el INSERT; solo el SELECT de retorno.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoAdjuntoIns]
    @ComunicadoId int,
    @NombreOriginal varchar(200),
    @ContentType varchar(80),
    @Contenido varbinary(max)
AS
BEGIN
    SET NOCOUNT ON;
    IF @Contenido IS NULL OR DATALENGTH(@Contenido) < 1
    BEGIN
        RAISERROR(N'Archivo vacío.', 16, 1);
        RETURN;
    END;
    INSERT INTO MAESTRO.ComunicadoAdjunto (
        ComunicadoId, NombreOriginal, ContentType, TamanoBytes, Contenido)
    VALUES (
        @ComunicadoId,
        LEFT(@NombreOriginal, 200),
        LEFT(@ContentType, 80),
        DATALENGTH(@Contenido),
        @Contenido);
    SELECT CAST(SCOPE_IDENTITY() AS int) AS AdjuntoId;
END
GO
