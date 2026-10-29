-- LAB Azure CREDITO de prueba (la de Vercel). NO producción.
-- 8 DNI con bienes: cola de remate + 3 remates vía USP (artículos PRM EN_ALMACEN).
-- Los otros 5 quedan para pulsar Rematar en la SPA. No vende ni cobra.
--
-- SSMS → F5. Si el primer SELECT no son estos 8, STOP (no sigas con @Aplicar=1).

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @AplicarCola   BIT = 1; -- 0 = solo lista
DECLARE @RematarTop    INT = 3; -- 0 = no llama usp_RematarPrenda (solo fechas)
DECLARE @Hoy DATE = CONVERT(date, dbo.ufnFecha());
DECLARE @UsuarioId INT;

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

SELECT TOP (1) @UsuarioId = u.UsuarioId
FROM MAESTRO.Usuario AS u
WHERE u.Estado = CAST(1 AS bit)
  AND u.NombreUsuario IN (N'ADMVENDIX', N'RMANTILLA')
ORDER BY CASE u.NombreUsuario WHEN N'ADMVENDIX' THEN 0 ELSE 1 END;

IF @UsuarioId IS NULL
    SELECT TOP (1) @UsuarioId = UsuarioId FROM MAESTRO.Usuario WHERE Estado = CAST(1 AS bit) ORDER BY UsuarioId;

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
    alm.AlmacenId,
    @UsuarioId AS UsuarioRemate
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
    PRINT N'Solo lectura. Pon @AplicarCola = 1 y vuelve a F5.';
    RETURN;
END;

BEGIN TRAN;

DECLARE @Batch TABLE (
    CreditoId INT PRIMARY KEY,
    OficinaId INT NOT NULL,
    PrendaId BIGINT NOT NULL,
    Rn INT NOT NULL
);

INSERT INTO @Batch (CreditoId, OficinaId, PrendaId, Rn)
SELECT CreditoId, OficinaId, PrendaId, n
FROM (
    SELECT
        c.CreditoId,
        c.OficinaId,
        p.PrendaId,
        ROW_NUMBER() OVER (ORDER BY pe.NombreCompleto, c.CreditoId) AS n
    FROM (
        SELECT
            c.CreditoId,
            c.OficinaId,
            c.PersonaId,
            ROW_NUMBER() OVER (PARTITION BY pe.NumeroDocumento ORDER BY c.CreditoId DESC) AS rn
        FROM @Dni AS d
        INNER JOIN MAESTRO.Persona AS pe ON pe.NumeroDocumento = d.NumeroDocumento
        INNER JOIN CREDITO.Credito AS c ON c.PersonaId = pe.PersonaId
        WHERE (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
          AND c.FechaRemateEjecutado IS NULL
          AND EXISTS (
                SELECT 1 FROM CREDITO.Prenda AS pr
                WHERE pr.CreditoId = c.CreditoId AND pr.Estado = N'EN CUSTODIA'
          )
    ) AS c
    INNER JOIN MAESTRO.Persona AS pe ON pe.PersonaId = c.PersonaId
    INNER JOIN CREDITO.Prenda AS p
        ON p.CreditoId = c.CreditoId AND p.Estado = N'EN CUSTODIA'
    WHERE c.rn = 1
) AS x;

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

IF @RematarTop > 0 AND @UsuarioId IS NOT NULL
BEGIN
    DECLARE @PrendaId BIGINT, @OficinaId INT;

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT TOP (@RematarTop) PrendaId, OficinaId
        FROM @Batch
        ORDER BY Rn;

    OPEN cur;
    FETCH NEXT FROM cur INTO @PrendaId, @OficinaId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC CREDITO.usp_RematarPrenda
            @PrendaId = @PrendaId,
            @OficinaId = @OficinaId,
            @UsuarioId = @UsuarioId;
        FETCH NEXT FROM cur INTO @PrendaId, @OficinaId;
    END;
    CLOSE cur;
    DEALLOCATE cur;
END;

COMMIT;

SELECT
    pe.NumeroDocumento,
    pe.NombreCompleto,
    c.CreditoId,
    c.Estado,
    CONVERT(date, c.FechaRemate) AS Remate,
    CAST(CASE WHEN c.FechaRemateEjecutado IS NULL THEN 1 ELSE 0 END AS bit) AS PuedeRematarEnUi,
    p.PrendaId,
    p.Estado AS PrendaEstado,
    art.CodArticulo,
    art.Denominacion AS Articulo,
    sa.NumeroSerie,
    sa.EstadoId AS SerieEstadoId -- 2 = EN_ALMACEN
FROM @Batch AS b
INNER JOIN CREDITO.Credito AS c ON c.CreditoId = b.CreditoId
INNER JOIN MAESTRO.Persona AS pe ON pe.PersonaId = c.PersonaId
INNER JOIN CREDITO.Prenda AS p ON p.CreditoId = c.CreditoId
LEFT JOIN ALMACEN.Articulo AS art ON art.ArticuloId = p.ArticuloRemateId
LEFT JOIN ALMACEN.SerieArticulo AS sa ON sa.ArticuloId = art.ArticuloId AND sa.EstadoId = 2
ORDER BY PuedeRematarEnUi DESC, pe.NombreCompleto;

PRINT N'Cola + remates SQL listos. SPA: tarjeta Remate (los que PuedeRematarEnUi=1) y Maestros/Artículos PRM* (stock 1).';
