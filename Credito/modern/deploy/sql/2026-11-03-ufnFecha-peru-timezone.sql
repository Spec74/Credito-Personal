/*
  Hora operativa = Perú (SA Pacific / Lima), sin offset rígido (+2 / -5).
  Independiente del huso del servidor (Azure SQL = UTC).

  Ejecutar en Azure de PRUEBA (y luego prod cuando toque).
  Idempotente.

  Nota: ufn_FechaGerencial está referenciada por defaults
  (DF_CierreGerencial_Fecha, DF_MetaGerencial_Fecha). Se sueltan,
  se altera la función y se recrean.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER FUNCTION [dbo].[ufnFecha]()
RETURNS DATETIME
AS
BEGIN
    -- UTC → hora civil Perú (sin DST). No usar GETDATE()+N.
    RETURN CONVERT(
        DATETIME,
        (SYSUTCDATETIME() AT TIME ZONE N'UTC') AT TIME ZONE N'SA Pacific Standard Time'
    );
END
GO

/* Defaults que bloquean ALTER de la UDF */
DECLARE @sql nvarchar(max) = N'';

SELECT @sql = @sql + N'ALTER TABLE '
    + QUOTENAME(OBJECT_SCHEMA_NAME(dc.parent_object_id)) + N'.'
    + QUOTENAME(OBJECT_NAME(dc.parent_object_id))
    + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(10)
FROM sys.default_constraints AS dc
WHERE dc.definition LIKE N'%ufn_FechaGerencial%';

IF LEN(@sql) > 0
BEGIN
    PRINT N'Soltando defaults que referencian ufn_FechaGerencial...';
    EXEC sys.sp_executesql @sql;
END
GO

CREATE OR ALTER FUNCTION CREDITO.ufn_FechaGerencial()
RETURNS DATETIME2(0)
AS
BEGIN
    RETURN CONVERT(DATETIME2(0), dbo.ufnFecha());
END
GO

/* Recrear defaults conocidos (si existen las tablas) */
IF OBJECT_ID(N'CREDITO.CierreGerencial', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.default_constraints
       WHERE name = N'DF_CierreGerencial_Fecha'
         AND parent_object_id = OBJECT_ID(N'CREDITO.CierreGerencial')
   )
BEGIN
    ALTER TABLE CREDITO.CierreGerencial
        ADD CONSTRAINT DF_CierreGerencial_Fecha
        DEFAULT (CREDITO.ufn_FechaGerencial()) FOR FechaCierre;
END
GO

IF OBJECT_ID(N'CREDITO.MetaGerencialAnalista', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.default_constraints
       WHERE name = N'DF_MetaGerencial_Fecha'
         AND parent_object_id = OBJECT_ID(N'CREDITO.MetaGerencialAnalista')
   )
BEGIN
    ALTER TABLE CREDITO.MetaGerencialAnalista
        ADD CONSTRAINT DF_MetaGerencial_Fecha
        DEFAULT (CREDITO.ufn_FechaGerencial()) FOR FechaRegistro;
END
GO

-- Humo: debe coincidir con hora Perú (±1 min vs reloj local)
SELECT
    SYSUTCDATETIME() AS UtcAhora,
    GETDATE() AS GetDateServidor,
    dbo.ufnFecha() AS FechaOperativaPeru,
    CREDITO.ufn_FechaGerencial() AS FechaGerencialPeru;
GO

PRINT N'OK: dbo.ufnFecha y CREDITO.ufn_FechaGerencial → hora Perú (SA Pacific Standard Time).';
GO
