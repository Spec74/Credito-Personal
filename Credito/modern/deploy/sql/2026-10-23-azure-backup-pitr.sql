-- SOLO LECTURA. No actualiza CREDITO. No aplica PLA, índices ni USP.
-- Columnas oficiales de Azure SQL Database (no hay backup_size).
-- Docs: sys.dm_database_backups (azuresqldb-current).
-- Pegar en Azure Query Editor de la base CREDITO.
-- Esperado: edition Azure; filas D/I/L; varias in_retention = 1.
-- Hyperscale: esta DMV no devuelve filas; confirmar en Portal → Backups.

SET NOCOUNT ON;

SELECT
    DB_NAME() AS base,
    CAST(DATABASEPROPERTYEX(DB_NAME(), N'Edition') AS nvarchar(64)) AS edition,
    CAST(DATABASEPROPERTYEX(DB_NAME(), N'ServiceObjective') AS nvarchar(64)) AS service_objective,
    d.recovery_model_desc,
    d.compatibility_level
FROM sys.databases AS d
WHERE d.database_id = DB_ID();
GO

SELECT TOP (20)
    backup_start_date,
    backup_finish_date,
    CASE backup_type
        WHEN N'D' THEN N'Full'
        WHEN N'I' THEN N'Differential'
        WHEN N'L' THEN N'Transaction log'
        ELSE backup_type
    END AS backup_type,
    CASE in_retention
        WHEN 1 THEN N'In retention'
        WHEN 0 THEN N'Out of retention'
        ELSE N'(null)'
    END AS in_retention
FROM sys.dm_database_backups
ORDER BY backup_start_date DESC;
GO

-- Portal (Informática): SQL database → Backups → PITR ≥ 7 días.
-- No restaurar sobre CREDITO de producción.
