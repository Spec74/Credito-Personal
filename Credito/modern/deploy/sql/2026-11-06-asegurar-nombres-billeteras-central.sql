-- Asegura nombres legibles de billeteras Central (idempotente).
-- No cambia ItemId ni saldos: solo Denominacion/DesCorta del catálogo ValorTabla 13.
-- Gerente/René son SOLO Central; genéricos 2/3 quedan para sucursal.

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

UPDATE MAESTRO.ValorTabla
SET Denominacion = N'YAPE', DesCorta = NULL, Valor = NULL
WHERE TablaId = 13 AND ItemId = 2;

UPDATE MAESTRO.ValorTabla
SET Denominacion = N'INTERBANK', DesCorta = NULL, Valor = NULL
WHERE TablaId = 13 AND ItemId = 3;

UPDATE MAESTRO.ValorTabla
SET Denominacion = N'YAPE GERENTE', DesCorta = N'CENTRAL', Valor = N'2'
WHERE TablaId = 13 AND ItemId = 10;

UPDATE MAESTRO.ValorTabla
SET Denominacion = N'YAPE RENE', DesCorta = N'CENTRAL', Valor = N'2'
WHERE TablaId = 13 AND ItemId = 11;

UPDATE MAESTRO.ValorTabla
SET Denominacion = N'INTERBANK GERENTE', DesCorta = N'CENTRAL', Valor = N'3'
WHERE TablaId = 13 AND ItemId = 12;

UPDATE MAESTRO.ValorTabla
SET Denominacion = N'INTERBANK RENE', DesCorta = N'CENTRAL', Valor = N'3'
WHERE TablaId = 13 AND ItemId = 13;

UPDATE MAESTRO.ValorTabla
SET Denominacion = N'PLIN', DesCorta = NULL
WHERE TablaId = 13 AND ItemId = 9;

SELECT ItemId, Denominacion, DesCorta, Valor
FROM MAESTRO.ValorTabla
WHERE TablaId = 13 AND ItemId IN (1, 2, 3, 4, 5, 9, 10, 11, 12, 13)
ORDER BY ItemId;
GO
