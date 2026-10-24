-- Índices de caminos calientes (rechazo PEN, caja del día, bóveda, PLA).
-- No toca USP ni % . Idempotente. Ventana corta: tablas hijas son chicas
-- salvo CxC (~75k) y BovedaMov (~67k).
-- No indexa las 87 FK de almacén.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Cargo_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.Cargo')
)
    CREATE NONCLUSTERED INDEX IX_Cargo_CreditoId ON CREDITO.Cargo (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Tarea_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.Tarea')
)
    CREATE NONCLUSTERED INDEX IX_Tarea_CreditoId ON CREDITO.Tarea (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CreditoImagen_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.CreditoImagen')
)
    CREATE NONCLUSTERED INDEX IX_CreditoImagen_CreditoId ON CREDITO.CreditoImagen (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CentralRiesgo_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.CentralRiesgo')
)
    CREATE NONCLUSTERED INDEX IX_CentralRiesgo_CreditoId ON CREDITO.CentralRiesgo (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CreditoMora_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.CreditoMora')
)
    CREATE NONCLUSTERED INDEX IX_CreditoMora_CreditoId ON CREDITO.CreditoMora (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CuentaxCobrar_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.CuentaxCobrar')
)
    CREATE NONCLUSTERED INDEX IX_CuentaxCobrar_CreditoId ON CREDITO.CuentaxCobrar (CreditoId)
        INCLUDE (Estado, Monto);
GO

IF OBJECT_ID(N'CREDITO.PlaInusual', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PlaInusual_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.PlaInusual')
)
    CREATE NONCLUSTERED INDEX IX_PlaInusual_CreditoId ON CREDITO.PlaInusual (CreditoId)
        WHERE CreditoId IS NOT NULL;
GO

IF OBJECT_ID(N'CREDITO.PlaInusual', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PlaInusual_Credito'
)
    ALTER TABLE CREDITO.PlaInusual WITH CHECK
        ADD CONSTRAINT FK_PlaInusual_Credito
        FOREIGN KEY (CreditoId) REFERENCES CREDITO.Credito (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CajaDiario_Caja_IndCierre' AND object_id = OBJECT_ID(N'CREDITO.CajaDiario')
)
    CREATE NONCLUSTERED INDEX IX_CajaDiario_Caja_IndCierre
        ON CREDITO.CajaDiario (CajaId, IndCierre)
        INCLUDE (TransBoveda, UsuarioAsignadoId, FechaIniOperacion);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Caja_OficinaId' AND object_id = OBJECT_ID(N'CREDITO.Caja')
)
    CREATE NONCLUSTERED INDEX IX_Caja_OficinaId ON CREDITO.Caja (OficinaId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_BovedaMov_BovedaId' AND object_id = OBJECT_ID(N'CREDITO.BovedaMov')
)
    CREATE NONCLUSTERED INDEX IX_BovedaMov_BovedaId ON CREDITO.BovedaMov (BovedaId, Estado)
        INCLUDE (Importe, IndEntrada, FechaReg);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_BovedaMov_CajaDiarioId' AND object_id = OBJECT_ID(N'CREDITO.BovedaMov')
)
    CREATE NONCLUSTERED INDEX IX_BovedaMov_CajaDiarioId ON CREDITO.BovedaMov (CajaDiarioId)
        WHERE CajaDiarioId IS NOT NULL;
GO

DECLARE @fkRolMenu sysname;
DECLARE @sqlRolMenu nvarchar(400);

SELECT @fkRolMenu = name
FROM sys.foreign_keys
WHERE parent_object_id = OBJECT_ID(N'MAESTRO.RolMenu')
  AND is_not_trusted = 1;

IF @fkRolMenu IS NOT NULL
BEGIN
    SET @sqlRolMenu = N'ALTER TABLE MAESTRO.RolMenu WITH CHECK CHECK CONSTRAINT ' + QUOTENAME(@fkRolMenu);
    EXEC (@sqlRolMenu);
END
GO
