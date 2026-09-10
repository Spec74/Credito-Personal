-- Índice opcional para el tablero del analista (cobranza por rango de FechaReg).
-- MovimientoCaja solo tiene IX por CreditoId+Operacion+Estado; el ranking y el
-- gráfico filtran por fecha y en el legado eso terminaba en timeout.
-- Ejecutar en ventana de mantenimiento. No es obligatorio para que el API funcione.

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_MovimientoCaja_FechaReg_CUO'
      AND object_id = OBJECT_ID(N'CREDITO.MovimientoCaja')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MovimientoCaja_FechaReg_CUO
        ON CREDITO.MovimientoCaja (FechaReg, CreditoId)
        INCLUDE (ImportePago)
        WHERE Operacion = 'CUO' AND Estado = 1 AND ImportePago > 0;
END
GO
