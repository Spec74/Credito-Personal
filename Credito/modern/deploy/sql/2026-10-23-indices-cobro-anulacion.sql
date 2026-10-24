-- Índices de cobro / anulación / sesión de caja.
-- Complementa caminos calientes + consulta/login. Idempotente. No toca USP.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CuentaxCobrar_MovimientoCajaId'
      AND object_id = OBJECT_ID(N'CREDITO.CuentaxCobrar')
)
    CREATE NONCLUSTERED INDEX IX_CuentaxCobrar_MovimientoCajaId
        ON CREDITO.CuentaxCobrar (MovimientoCajaId)
        WHERE MovimientoCajaId IS NOT NULL;
GO

IF OBJECT_ID(N'CREDITO.MovimientoCajaAnu', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_MovimientoCajaAnu_MovimientoCajaId'
      AND object_id = OBJECT_ID(N'CREDITO.MovimientoCajaAnu')
)
    CREATE NONCLUSTERED INDEX IX_MovimientoCajaAnu_MovimientoCajaId
        ON CREDITO.MovimientoCajaAnu (MovimientoCajaId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CajaDiario_UsuarioAsignadoId'
      AND object_id = OBJECT_ID(N'CREDITO.CajaDiario')
)
    CREATE NONCLUSTERED INDEX IX_CajaDiario_UsuarioAsignadoId
        ON CREDITO.CajaDiario (UsuarioAsignadoId)
        INCLUDE (IndCierre, CajaId)
        WHERE UsuarioAsignadoId IS NOT NULL;
GO
