-- LAB — cola de remate para probar el botón Rematar en la SPA (no llama usp_RematarPrenda).
-- Solo 8 DNI con bienes. Omite «Sin bienes».
-- SSMS → la CREDITO de prueba (la de Vercel/API) → F5 el SELECT → si cuadra, descomenta el UPDATE.

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Hoy DATE = CONVERT(date, dbo.ufnFecha());

DECLARE @Dni TABLE (NumeroDocumento NVARCHAR(20) PRIMARY KEY);
INSERT INTO @Dni (NumeroDocumento) VALUES
    (N'76738856'), -- FERNANDEZ LEON, RUTH MARIA
    (N'20719288'), -- PALOMINO TORRES, RAUL DAVID
    (N'28232176'), -- PAREDES DE SULCA, FELICITAS
    (N'28227125'), -- CONTRERAS ESCALANTE, JUSTINA
    (N'77403936'), -- RAMIREZ MORENO, JARED ARMANDO
    (N'75252746'), -- MEZA VILLALBA, FABIOLA YOMAIRA
    (N'74039361'), -- SULCA PAREDES, victor
    (N'43278292'); -- VILLALBA VILLANUEVA, LEIDA GABY

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
        ROW_NUMBER() OVER (
            PARTITION BY pe.NumeroDocumento
            ORDER BY c.CreditoId DESC
        ) AS rn
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
    (SELECT COUNT(*) FROM CREDITO.Prenda p WHERE p.CreditoId = cand.CreditoId AND p.Estado = N'EN CUSTODIA') AS BienesCustodia,
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

/* ========== Aplicar cola (descomentar) — luego Rematar en la interfaz ==========
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

IF (SELECT COUNT(*) FROM @Batch) < 1
BEGIN
    ROLLBACK;
    THROW 50003, N'Ningún DNI de la lista tiene prendario EN CUSTODIA sin remate.', 1;
END;

INSERT INTO ALMACEN.Almacen (OficinaId, Denominacion, Descripcion, IndEstadoApertura, FechaApertura, Estado)
SELECT DISTINCT b.OficinaId, N'ALMACEN LAB', N'Lab cola remate UI', CAST(1 AS bit), dbo.ufnFecha(), CAST(1 AS bit)
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
    CONVERT(date, c.FechaVencimiento) AS Vence,
    CONVERT(date, c.FechaRemate) AS Remate,
    CAST(CASE
        WHEN c.Estado = N'DES'
         AND c.FechaRemateEjecutado IS NULL
         AND c.FechaRemate < @Hoy
        THEN 1 ELSE 0 END AS bit) AS PuedeRematarEnUi
FROM @Batch AS b
INNER JOIN CREDITO.Credito AS c ON c.CreditoId = b.CreditoId
INNER JOIN MAESTRO.Persona AS pe ON pe.PersonaId = c.PersonaId
ORDER BY pe.NombreCompleto;

-- COMMIT;
-- ROLLBACK;
========== */
