/*
  Oficina.UsuarioAsignadoId opcional
  ----------------------------------
  Rompe el círculo oficina↔usuario: la sede puede crearse sin responsable
  (o con el admin en sesión). El responsable se asigna después.

  Idempotente. Ejecutar en local / Azure prueba / prod.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'MAESTRO.Oficina', N'UsuarioAsignadoId') IS NULL
BEGIN
    PRINT N'MAESTRO.Oficina.UsuarioAsignadoId no existe; nada que hacer.';
    RETURN;
END;

/* Quitar DEFAULT legado ((3)) si existe. */
DECLARE @df sysname;
SELECT @df = dc.name
FROM sys.default_constraints AS dc
INNER JOIN sys.columns AS c
    ON c.object_id = dc.parent_object_id
   AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'MAESTRO.Oficina')
  AND c.name = N'UsuarioAsignadoId';

IF @df IS NOT NULL
BEGIN
    DECLARE @sql nvarchar(400) =
        N'ALTER TABLE MAESTRO.Oficina DROP CONSTRAINT ' + QUOTENAME(@df) + N';';
    EXEC sys.sp_executesql @sql;
    PRINT N'DROP DEFAULT ' + @df;
END;

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'MAESTRO.Oficina')
      AND name = N'UsuarioAsignadoId'
      AND is_nullable = 0)
BEGIN
    ALTER TABLE MAESTRO.Oficina ALTER COLUMN UsuarioAsignadoId int NULL;
    PRINT N'UsuarioAsignadoId ahora admite NULL.';
END
ELSE
    PRINT N'UsuarioAsignadoId ya era nullable.';

SELECT
    c.name AS Columna,
    c.is_nullable AS IsNullable,
    t.name AS Tipo
FROM sys.columns AS c
INNER JOIN sys.types AS t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'MAESTRO.Oficina')
  AND c.name = N'UsuarioAsignadoId';
GO
