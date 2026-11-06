-- Catálogo + cuentas Central para medios operativos (idempotente).
-- Crea si faltan: Plin(9), Yape Gerente/René(10/11), Interbank Gerente/René(12/13).
-- El UPDATE solo NO alcanza: sin INSERT el combo no muestra Interbank.
-- Genéricos 2/3 quedan para sucursal. Billeteras CENTRAL solo en IndPrincipal.
--
-- Azure / producción — correr EN ESTE ORDEN:
--   1) este script
--   2) 2026-11-06-usp-transferir-boveda-map-billeteras.sql
--   3) si hubo saldos previos en 2/3 o billeteras en sucursal:
--      2026-11-06-central-billeteras-yape-interbank.sql

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

UPDATE MAESTRO.ValorTabla
SET Denominacion = N'YAPE', DesCorta = NULL, Valor = NULL
WHERE TablaId = 13 AND ItemId = 2;

UPDATE MAESTRO.ValorTabla
SET Denominacion = N'INTERBANK', DesCorta = NULL, Valor = NULL
WHERE TablaId = 13 AND ItemId = 3;

IF NOT EXISTS (SELECT 1 FROM MAESTRO.ValorTabla WHERE TablaId = 13 AND ItemId = 9)
    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
    VALUES (13, 9, N'PLIN', NULL, NULL);
ELSE
    UPDATE MAESTRO.ValorTabla
    SET Denominacion = N'PLIN',
        DesCorta = CASE WHEN ISNULL(DesCorta, N'') = N'OBSOLETO' THEN NULL ELSE DesCorta END
    WHERE TablaId = 13 AND ItemId = 9;

IF NOT EXISTS (SELECT 1 FROM MAESTRO.ValorTabla WHERE TablaId = 13 AND ItemId = 10)
    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
    VALUES (13, 10, N'YAPE GERENTE', N'CENTRAL', N'2');
ELSE
    UPDATE MAESTRO.ValorTabla
    SET Denominacion = N'YAPE GERENTE', DesCorta = N'CENTRAL', Valor = N'2'
    WHERE TablaId = 13 AND ItemId = 10;

IF NOT EXISTS (SELECT 1 FROM MAESTRO.ValorTabla WHERE TablaId = 13 AND ItemId = 11)
    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
    VALUES (13, 11, N'YAPE RENE', N'CENTRAL', N'2');
ELSE
    UPDATE MAESTRO.ValorTabla
    SET Denominacion = N'YAPE RENE', DesCorta = N'CENTRAL', Valor = N'2'
    WHERE TablaId = 13 AND ItemId = 11;

IF NOT EXISTS (SELECT 1 FROM MAESTRO.ValorTabla WHERE TablaId = 13 AND ItemId = 12)
    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
    VALUES (13, 12, N'INTERBANK GERENTE', N'CENTRAL', N'3');
ELSE
    UPDATE MAESTRO.ValorTabla
    SET Denominacion = N'INTERBANK GERENTE', DesCorta = N'CENTRAL', Valor = N'3'
    WHERE TablaId = 13 AND ItemId = 12;

IF NOT EXISTS (SELECT 1 FROM MAESTRO.ValorTabla WHERE TablaId = 13 AND ItemId = 13)
    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
    VALUES (13, 13, N'INTERBANK RENE', N'CENTRAL', N'3');
ELSE
    UPDATE MAESTRO.ValorTabla
    SET Denominacion = N'INTERBANK RENE', DesCorta = N'CENTRAL', Valor = N'3'
    WHERE TablaId = 13 AND ItemId = 13;
GO

DECLARE @OficinaCentralId INT = (
    SELECT TOP (1) OficinaId
    FROM MAESTRO.Oficina
    WHERE IndPrincipal = CAST(1 AS bit) AND Estado = CAST(1 AS bit)
    ORDER BY OficinaId
);

-- Plin en todas las bóvedas abiertas
INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
SELECT b.BovedaId, 9, CAST(0 AS decimal(16, 2))
FROM CREDITO.Boveda AS b
WHERE b.IndCierre = CAST(0 AS bit)
  AND NOT EXISTS (
      SELECT 1 FROM CREDITO.BovedaCuenta AS bc
      WHERE bc.BovedaId = b.BovedaId AND bc.TipoPagoId = 9
  );

-- Billeteras Gerente/René solo en Central
IF @OficinaCentralId IS NOT NULL
BEGIN
    INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
    SELECT b.BovedaId, v.ItemId, CAST(0 AS decimal(16, 2))
    FROM CREDITO.Boveda AS b
    CROSS JOIN (VALUES (10), (11), (12), (13)) AS v(ItemId)
    WHERE b.IndCierre = CAST(0 AS bit)
      AND b.OficinaId = @OficinaCentralId
      AND NOT EXISTS (
          SELECT 1 FROM CREDITO.BovedaCuenta AS bc
          WHERE bc.BovedaId = b.BovedaId AND bc.TipoPagoId = v.ItemId
      );
END
GO

SELECT ItemId, Denominacion, DesCorta, Valor
FROM MAESTRO.ValorTabla
WHERE TablaId = 13 AND ItemId IN (1, 2, 3, 4, 5, 9, 10, 11, 12, 13)
ORDER BY ItemId;
GO
