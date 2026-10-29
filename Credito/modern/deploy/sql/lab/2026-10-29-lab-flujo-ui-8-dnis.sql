-- LAB — preparar 8 prendarios CON BIENES para probar el flujo en la SPA.
-- Pone cola de remate (DES + FechaRemate ayer). NO remata, NO vende, NO arma kardex.
-- Kardex = movimientos de Entrada/Salida. El remate deja serie EN_ALMACEN sin movimiento.
--
-- 1) F5 con @AplicarCola = 0  → lista.
-- 2) @AplicarCola = 1         → COMMIT de la cola.
-- 3) En Credix sigue el orden del comentario «SPA».

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @AplicarCola BIT = 0; -- cámbialo a 1 cuando el SELECT esté bien
DECLARE @Hoy DATE = CONVERT(date, dbo.ufnFecha());

DECLARE @Dni TABLE (NumeroDocumento NVARCHAR(20) PRIMARY KEY);
INSERT INTO @Dni (NumeroDocumento) VALUES
    (N'76738856'),
    (N'20719288'),
    (N'28232176'),
    (N'28227125'),
    (N'77403936'),
    (N'75252746'),
    (N'74039361'),
    (N'43278292');

;WITH cand AS (
    SELECT
        c.CreditoId,
        c.OficinaId,
        c.Estado,
        c.FechaVencimiento,
        c.FechaRemate,
        c.FechaRemateEjecutado,
        pe.NumeroDocumento,
        pe.NombreCompleto,
        ROW_NUMBER() OVER (PARTITION BY pe.NumeroDocumento ORDER BY c.CreditoId DESC) AS rn
    FROM @Dni AS d
    INNER JOIN MAESTRO.Persona AS pe ON pe.NumeroDocumento = d.NumeroDocumento
    INNER JOIN CREDITO.Credito AS c ON c.PersonaId = pe.PersonaId
    WHERE (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
      AND c.FechaRemateEjecutado IS NULL
      AND EXISTS (
            SELECT 1 FROM CREDITO.Prenda AS p
            WHERE p.CreditoId = c.CreditoId AND p.Estado = N'EN CUSTODIA'
      )
)
SELECT
    cand.NumeroDocumento,
    cand.NombreCompleto,
    cand.CreditoId,
    cand.OficinaId,
    cand.Estado,
    CONVERT(date, cand.FechaVencimiento) AS Vence,
    CONVERT(date, cand.FechaRemate) AS Remate,
    @Hoy AS HoyBd,
    (SELECT COUNT(*) FROM CREDITO.Prenda p
     WHERE p.CreditoId = cand.CreditoId AND p.Estado = N'EN CUSTODIA') AS BienesCustodia,
    alm.AlmacenId
FROM cand
OUTER APPLY (
    SELECT TOP (1) a.AlmacenId
    FROM ALMACEN.Almacen AS a
    WHERE a.OficinaId = cand.OficinaId AND a.Estado = CAST(1 AS bit)
    ORDER BY a.AlmacenId
) AS alm
WHERE cand.rn = 1
ORDER BY cand.NombreCompleto;

IF @AplicarCola = 0
BEGIN
    PRINT N'Solo lectura. Pon @AplicarCola = 1 y vuelve a ejecutar para dejar la cola.';
    RETURN;
END;

BEGIN TRAN;

DECLARE @Batch TABLE (CreditoId INT PRIMARY KEY, OficinaId INT NOT NULL);

INSERT INTO @Batch (CreditoId, OficinaId)
SELECT CreditoId, OficinaId
FROM (
    SELECT
        c.CreditoId,
        c.OficinaId,
        ROW_NUMBER() OVER (PARTITION BY pe.NumeroDocumento ORDER BY c.CreditoId DESC) AS rn
    FROM @Dni AS d
    INNER JOIN MAESTRO.Persona AS pe ON pe.NumeroDocumento = d.NumeroDocumento
    INNER JOIN CREDITO.Credito AS c ON c.PersonaId = pe.PersonaId
    WHERE (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
      AND c.FechaRemateEjecutado IS NULL
      AND EXISTS (
            SELECT 1 FROM CREDITO.Prenda AS p
            WHERE p.CreditoId = c.CreditoId AND p.Estado = N'EN CUSTODIA'
      )
) AS x
WHERE x.rn = 1;

IF NOT EXISTS (SELECT 1 FROM @Batch)
BEGIN
    ROLLBACK;
    THROW 50003, N'Ningún DNI tiene prendario EN CUSTODIA sin remate.', 1;
END;

INSERT INTO ALMACEN.Almacen (OficinaId, Denominacion, Descripcion, IndEstadoApertura, FechaApertura, Estado)
SELECT DISTINCT b.OficinaId, N'ALMACEN LAB', N'Lab flujo UI', CAST(1 AS bit), dbo.ufnFecha(), CAST(1 AS bit)
FROM @Batch AS b
WHERE NOT EXISTS (
    SELECT 1 FROM ALMACEN.Almacen AS a
    WHERE a.OficinaId = b.OficinaId AND a.Estado = CAST(1 AS bit)
);

UPDATE c
SET Estado = N'DES',
    FechaVencimiento = DATEADD(DAY, -31, @Hoy),
    FechaRemate = DATEADD(DAY, -1, @Hoy),
    FechaRemateEjecutado = NULL,
    FechaMod = dbo.ufnFecha()
FROM CREDITO.Credito AS c
INNER JOIN @Batch AS b ON b.CreditoId = c.CreditoId;

SELECT
    pe.NumeroDocumento,
    pe.NombreCompleto,
    c.CreditoId,
    c.Estado,
    CONVERT(date, c.FechaRemate) AS Remate,
    CAST(1 AS bit) AS PuedeRematarEnUi
FROM @Batch AS b
INNER JOIN CREDITO.Credito AS c ON c.CreditoId = b.CreditoId
INNER JOIN MAESTRO.Persona AS pe ON pe.PersonaId = c.PersonaId
ORDER BY pe.NombreCompleto;

COMMIT;
PRINT N'Cola lista. En la SPA: Rematar (2 o 3) → Artículos/Stock → Venta rápida. Kardex: Entrada de almacén de un artículo de catálogo.';
