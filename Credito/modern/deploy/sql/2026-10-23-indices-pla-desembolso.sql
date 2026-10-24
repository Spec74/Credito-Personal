-- PLA: desembolsos del mes por sede (usp_PlaCandidatosRo).
-- ~77k filas. No toca MovimientoCaja (1,6 M). Idempotente.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Credito_Oficina_FechaDesembolso'
      AND object_id = OBJECT_ID(N'CREDITO.Credito')
)
    CREATE NONCLUSTERED INDEX IX_Credito_Oficina_FechaDesembolso
        ON CREDITO.Credito (OficinaId, FechaDesembolso)
        INCLUDE (PersonaId, MontoDesembolso, MontoCredito, FechaReg)
        WHERE FechaDesembolso IS NOT NULL;
GO
