-- Columna de control del aviso WhatsApp a 3 dias del vencimiento prendario.
-- Paridad CreditoBL.ObtenerCreditosPrendariosPorVencer / MarcarNotificadoWhatsapp.
-- Idempotente.

IF COL_LENGTH(N'CREDITO.Credito', N'FechaNotifWhatsapp3d') IS NULL
BEGIN
    ALTER TABLE CREDITO.Credito ADD FechaNotifWhatsapp3d datetime NULL;
END
GO
