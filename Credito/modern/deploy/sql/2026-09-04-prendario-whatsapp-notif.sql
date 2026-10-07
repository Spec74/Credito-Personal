-- Obsoleto: antes agregaba FechaNotifWhatsapp3d. Use 2026-10-06-prendario-drop-fecha-notif-whatsapp.sql
-- si la columna aún existe en bases restauradas.

IF COL_LENGTH(N'CREDITO.Credito', N'FechaNotifWhatsapp3d') IS NOT NULL
BEGIN
    ALTER TABLE CREDITO.Credito DROP COLUMN FechaNotifWhatsapp3d;
END
GO
