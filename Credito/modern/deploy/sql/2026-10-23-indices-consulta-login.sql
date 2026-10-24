-- Índices de consulta / login / condonación / bóveda / subtarea.
-- Complementa 2026-10-23-indices-caminos-calientes.sql (rechazo/caja).
-- No toca USP ni % . Idempotente. Tablas chicas salvo Credito (~77k).

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Credito_PersonaId' AND object_id = OBJECT_ID(N'CREDITO.Credito')
)
    CREATE NONCLUSTERED INDEX IX_Credito_PersonaId
        ON CREDITO.Credito (PersonaId)
        INCLUDE (Estado, OficinaId);
GO

IF OBJECT_ID(N'CREDITO.CreditoCondonacion', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CreditoCondonacion_CreditoId'
      AND object_id = OBJECT_ID(N'CREDITO.CreditoCondonacion')
)
    CREATE NONCLUSTERED INDEX IX_CreditoCondonacion_CreditoId
        ON CREDITO.CreditoCondonacion (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Boveda_OficinaId' AND object_id = OBJECT_ID(N'CREDITO.Boveda')
)
    CREATE NONCLUSTERED INDEX IX_Boveda_OficinaId ON CREDITO.Boveda (OficinaId);
GO

IF OBJECT_ID(N'CREDITO.BovedaCuenta', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_BovedaCuenta_BovedaId'
      AND object_id = OBJECT_ID(N'CREDITO.BovedaCuenta')
)
    CREATE NONCLUSTERED INDEX IX_BovedaCuenta_BovedaId
        ON CREDITO.BovedaCuenta (BovedaId);
GO

IF OBJECT_ID(N'CREDITO.Subtarea', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Subtarea_TareaId' AND object_id = OBJECT_ID(N'CREDITO.Subtarea')
)
    CREATE NONCLUSTERED INDEX IX_Subtarea_TareaId ON CREDITO.Subtarea (TareaId);
GO

IF OBJECT_ID(N'MAESTRO.Cliente', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Cliente_PersonaId' AND object_id = OBJECT_ID(N'MAESTRO.Cliente')
)
    CREATE NONCLUSTERED INDEX IX_Cliente_PersonaId ON MAESTRO.Cliente (PersonaId);
GO

IF OBJECT_ID(N'MAESTRO.UsuarioOficina', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_UsuarioOficina_Usuario_Oficina'
      AND object_id = OBJECT_ID(N'MAESTRO.UsuarioOficina')
)
    CREATE NONCLUSTERED INDEX IX_UsuarioOficina_Usuario_Oficina
        ON MAESTRO.UsuarioOficina (UsuarioId, OficinaId);
GO

IF OBJECT_ID(N'MAESTRO.UsuarioRol', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_UsuarioRol_Usuario_Oficina'
      AND object_id = OBJECT_ID(N'MAESTRO.UsuarioRol')
)
    CREATE NONCLUSTERED INDEX IX_UsuarioRol_Usuario_Oficina
        ON MAESTRO.UsuarioRol (UsuarioId, OficinaId)
        INCLUDE (RolId);
GO
