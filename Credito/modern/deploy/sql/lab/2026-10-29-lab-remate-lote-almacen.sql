-- LAB Azure — lote: créditos prendarios VENCIDOS → cola remate → usp_RematarPrenda → almacén
-- No mueve caja ni vende. Un crédito = todas sus prendas EN CUSTODIA (el USP las remata juntas).
--
-- Cómo: 1) ejecuta solo la sección A (lista). 2) si cuadra, descomenta B y C.
-- Ajusta @Limite. No corridas masivas.

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Limite INT = 8;
DECLARE @Hoy DATE = CONVERT(date, dbo.ufnFecha());
DECLARE @UsuarioId INT;

SELECT TOP (1) @UsuarioId = u.UsuarioId
FROM MAESTRO.Usuario AS u
WHERE u.Estado = CAST(1 AS bit)
  AND u.NombreUsuario IN (N'RMANTILLA', N'ADMVENDIX')
ORDER BY CASE u.NombreUsuario WHEN N'RMANTILLA' THEN 0 ELSE 1 END;

IF @UsuarioId IS NULL
    SELECT TOP (1) @UsuarioId = UsuarioId FROM MAESTRO.Usuario WHERE Estado = CAST(1 AS bit) ORDER BY UsuarioId;

/* ========== A) Candidatos (siempre) ========== */
SELECT TOP (@Limite)
    c.CreditoId,
    c.OficinaId,
    o.Denominacion AS Oficina,
    c.Estado,
    pe.NombreCompleto,
    pe.NumeroDocumento,
    c.FechaVencimiento,
    c.FechaRemate,
    @Hoy AS HoyBd,
    CAST(CASE WHEN COALESCE(c.FechaRemate, DATEADD(DAY, 30, c.FechaVencimiento)) < @Hoy THEN 1 ELSE 0 END AS bit) AS YaEnVentanaRemate,
    p.PrendaId,
    p.Descripcion,
    p.Estado AS PrendaEstado,
    p.ValorTasacion,
    alm.AlmacenId,
    CAST(CASE WHEN c.Estado = N'DES' THEN 1 ELSE 0 END AS bit) AS YaDesembolsado
FROM CREDITO.Credito AS c
INNER JOIN MAESTRO.Persona AS pe ON pe.PersonaId = c.PersonaId
LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = c.OficinaId
INNER JOIN CREDITO.Prenda AS p
    ON p.CreditoId = c.CreditoId AND p.Estado = N'EN CUSTODIA'
OUTER APPLY (
    SELECT TOP (1) a.AlmacenId
    FROM ALMACEN.Almacen AS a
    WHERE a.OficinaId = c.OficinaId AND a.Estado = CAST(1 AS bit)
    ORDER BY a.AlmacenId
) AS alm
WHERE (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
  AND c.FechaRemateEjecutado IS NULL
  AND c.FechaVencimiento < @Hoy
  AND c.Estado IN (N'DES', N'APR')
ORDER BY c.FechaVencimiento, c.CreditoId, p.PrendaId;

/* ========== B+C) Cola + remate (descomentar entero) ==========
BEGIN TRAN;

DECLARE @Batch TABLE (
    CreditoId INT NOT NULL PRIMARY KEY,
    OficinaId INT NOT NULL,
    PrendaId BIGINT NOT NULL,
    EstadoOrig NVARCHAR(10) NOT NULL,
    FechaVencOrig DATETIME NULL,
    FechaRemateOrig DATETIME NULL
);

INSERT INTO @Batch (CreditoId, OficinaId, PrendaId, EstadoOrig, FechaVencOrig, FechaRemateOrig)
SELECT TOP (@Limite)
    x.CreditoId, x.OficinaId, x.PrendaId, x.Estado, x.FechaVencimiento, x.FechaRemate
FROM (
    SELECT
        c.CreditoId,
        c.OficinaId,
        c.Estado,
        c.FechaVencimiento,
        c.FechaRemate,
        p.PrendaId,
        ROW_NUMBER() OVER (PARTITION BY c.CreditoId ORDER BY p.PrendaId) AS rn
    FROM CREDITO.Credito AS c
    INNER JOIN CREDITO.Prenda AS p
        ON p.CreditoId = c.CreditoId AND p.Estado = N'EN CUSTODIA'
    WHERE (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
      AND c.FechaRemateEjecutado IS NULL
      AND c.FechaVencimiento < @Hoy
      AND c.Estado IN (N'DES', N'APR')
) AS x
WHERE x.rn = 1
ORDER BY x.FechaVencimiento, x.CreditoId;

IF NOT EXISTS (SELECT 1 FROM @Batch)
BEGIN
    ROLLBACK;
    THROW 50002, N'No hay prendarios vencidos EN CUSTODIA para el lote.', 1;
END;

-- Almacén activo por sede del lote
INSERT INTO ALMACEN.Almacen (OficinaId, Denominacion, Descripcion, IndEstadoApertura, FechaApertura, Estado)
SELECT DISTINCT b.OficinaId, N'ALMACEN LAB', N'Alta lab remate lote', CAST(1 AS bit), dbo.ufnFecha(), CAST(1 AS bit)
FROM @Batch AS b
WHERE NOT EXISTS (
    SELECT 1 FROM ALMACEN.Almacen AS a
    WHERE a.OficinaId = b.OficinaId AND a.Estado = CAST(1 AS bit)
);

-- Cola: DES + FechaRemate ayer (el USP no remata APR ni fecha futura)
UPDATE c
SET Estado = N'DES',
    FechaRemate = DATEADD(DAY, -1, @Hoy),
    FechaRemateEjecutado = NULL,
    FechaMod = dbo.ufnFecha()
FROM CREDITO.Credito AS c
INNER JOIN @Batch AS b ON b.CreditoId = c.CreditoId;

DECLARE @CreditoId INT, @OficinaId INT, @PrendaId BIGINT;

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT CreditoId, OficinaId, PrendaId FROM @Batch;

OPEN cur;
FETCH NEXT FROM cur INTO @CreditoId, @OficinaId, @PrendaId;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC CREDITO.usp_RematarPrenda
        @PrendaId = @PrendaId,
        @OficinaId = @OficinaId,
        @UsuarioId = @UsuarioId;
    FETCH NEXT FROM cur INTO @CreditoId, @OficinaId, @PrendaId;
END
CLOSE cur;
DEALLOCATE cur;

SELECT
    b.CreditoId,
    c.Estado,
    c.FechaRemateEjecutado,
    p.PrendaId,
    p.Estado AS PrendaEstado,
    p.ArticuloRemateId,
    a.CodArticulo,
    a.Denominacion,
    s.NumeroSerie,
    s.EstadoId AS SerieEstadoId, -- 2 = EN_ALMACEN
    s.AlmacenId
FROM @Batch AS b
INNER JOIN CREDITO.Credito AS c ON c.CreditoId = b.CreditoId
INNER JOIN CREDITO.Prenda AS p ON p.CreditoId = b.CreditoId
LEFT JOIN ALMACEN.Articulo AS a ON a.ArticuloId = p.ArticuloRemateId
LEFT JOIN ALMACEN.SerieArticulo AS s ON s.ArticuloId = a.ArticuloId;

-- COMMIT;
-- ROLLBACK;
========== */
