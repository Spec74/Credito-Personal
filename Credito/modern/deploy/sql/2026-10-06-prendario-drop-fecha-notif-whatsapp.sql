-- Retira columna de idempotencia WhatsApp en CREDITO.Credito (no se usa en operación).
-- El aviso manual (wa.me) y Cloud API, si se activan, no persisten marca en BD.
-- Idempotente.

IF COL_LENGTH(N'CREDITO.Credito', N'FechaNotifWhatsapp3d') IS NOT NULL
BEGIN
    ALTER TABLE CREDITO.Credito DROP COLUMN FechaNotifWhatsapp3d;
END
GO
