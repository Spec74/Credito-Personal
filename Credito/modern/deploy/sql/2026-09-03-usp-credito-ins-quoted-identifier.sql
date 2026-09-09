-- Recrea CREDITO.usp_Credito_Ins con QUOTED_IDENTIFIER ON, sin cambiar el cuerpo.
--
-- El indice filtrado IX_Credito_EsPrendario (WHERE EsPrendario = 1) exige esas
-- opciones SET en cualquier UPDATE/INSERT sobre CREDITO.Credito. El procedimiento
-- del cliente nacio con QUOTED_IDENTIFIER OFF: al generar el credito (estado CRE
-- a PEN) SQL Server lanza el error 1934 y la API responde 503.
--
-- Idempotente: si el modulo ya tiene QUOTED_IDENTIFIER ON, no hace nada.

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'CREDITO.usp_Credito_Ins', N'P') IS NULL
BEGIN
    THROW 51000, 'CREDITO.usp_Credito_Ins no existe.', 1;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.sql_modules
    WHERE object_id = OBJECT_ID(N'CREDITO.usp_Credito_Ins')
      AND uses_quoted_identifier = 0
)
BEGIN
    DECLARE @def nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'CREDITO.usp_Credito_Ins'));
    IF @def IS NULL
    BEGIN
        THROW 51000, 'No se pudo leer OBJECT_DEFINITION de CREDITO.usp_Credito_Ins.', 1;
    END;

    DECLARE @pos int = PATINDEX(N'%PROC%', @def);
    IF @pos < 1
    BEGIN
        THROW 51000, 'OBJECT_DEFINITION de usp_Credito_Ins no contiene PROC.', 1;
    END;

    SET @def = N'CREATE OR ALTER ' + SUBSTRING(@def, @pos, LEN(@def));
    EXEC sys.sp_executesql @def;
END
GO
