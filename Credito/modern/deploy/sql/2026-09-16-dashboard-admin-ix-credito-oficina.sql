-- IX for dashboard admin filters by office
-- Ejecutar en ventana de mantenimiento. No es obligatorio para que el API funcione.

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Credito_Oficina_Estado_Desembolso'
      AND object_id = OBJECT_ID(N'CREDITO.Credito')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Credito_Oficina_Estado_Desembolso
        ON CREDITO.Credito (OficinaId, Estado, FechaDesembolso)
        INCLUDE (MontoDesembolso, UsuarioRegId, PersonaId, IndIrrecuperable, FechaVencimiento);
END
GO
