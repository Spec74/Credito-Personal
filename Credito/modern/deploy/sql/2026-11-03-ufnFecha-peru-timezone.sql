/*
  Hora operativa = Perú (SA Pacific / Lima), sin offset rígido (+2 / -5).
  Independiente del huso del servidor (Azure SQL = UTC).

  Ejecutar en Azure de PRUEBA (y luego prod cuando toque).
  Idempotente: CREATE OR ALTER.
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

CREATE OR ALTER FUNCTION CREDITO.ufn_FechaGerencial()
RETURNS DATETIME2(0)
AS
BEGIN
    RETURN CONVERT(DATETIME2(0), dbo.ufnFecha());
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
