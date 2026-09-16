-- Índices para tablero gerencial (detalle/cartera por oficina).
-- Ejecutar en ventana de mantenimiento. Idempotente.

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PlanPago_Estado_CreditoId'
      AND object_id = OBJECT_ID(N'CREDITO.PlanPago')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PlanPago_Estado_CreditoId
        ON CREDITO.PlanPago (Estado, CreditoId)
        INCLUDE (Cuota, Cargo, PagoCuota, PagoLibre, FechaVencimiento);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_MovimientoCaja_Operacion_Estado_Fecha'
      AND object_id = OBJECT_ID(N'CREDITO.MovimientoCaja')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MovimientoCaja_Operacion_Estado_Fecha
        ON CREDITO.MovimientoCaja (Operacion, Estado, FechaReg)
        INCLUDE (CreditoId, ImportePago, IndEntrada, CajaDiarioId);
END
GO
