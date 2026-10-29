-- LAB Azure — un solo crédito: RAMIREZ MORENO / DNI 77403936 / contrato 87695
-- Objetivo: que aparezca «Rematar» en Gestión prendaria (cola venc. + 30 d.).
-- NO ejecuta usp_RematarPrenda ni mueve caja. Después, en la SPA: Rematar → Venta rápida.
-- Hoy el crédito está APR y FechaRemate = 15/12/2026 (futuro). El USP exige DES y FechaRemate < ufnFecha().
--
-- SSMS → CREDITO (Azure) → revisar el SELECT 1 → descomentar el UPDATE → F5.
-- Reversión: bloque al final.

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @CreditoId INT = 87695;
DECLARE @Dni VARCHAR(20) = N'77403936';

/* ========== 1) Ver qué hay (siempre) ========== */
SELECT
    c.CreditoId,
    c.OficinaId,
    c.Estado,
    c.EsPrendario,
    c.ProductoId,
    c.FechaVencimiento,
    c.FechaRemate,
    c.FechaRemateEjecutado,
    CONVERT(date, dbo.ufnFecha()) AS HoyBd,
    CAST(CASE
        WHEN c.Estado = N'DES'
         AND c.FechaRemateEjecutado IS NULL
         AND COALESCE(c.FechaRemate, DATEADD(DAY, 30, c.FechaVencimiento)) < CONVERT(date, dbo.ufnFecha())
        THEN 1 ELSE 0 END AS bit) AS PuedeRematarAhora,
    p.PrendaId,
    p.Descripcion,
    p.Estado AS PrendaEstado,
    p.ValorTasacion,
    pe.NombreCompleto,
    pe.NumeroDocumento
FROM CREDITO.Credito AS c
INNER JOIN CREDITO.Prenda AS p ON p.CreditoId = c.CreditoId
INNER JOIN MAESTRO.Persona AS pe ON pe.PersonaId = c.PersonaId
WHERE c.CreditoId = @CreditoId;

SELECT a.AlmacenId, a.OficinaId, a.Denominacion, a.Estado
FROM ALMACEN.Almacen AS a
WHERE a.OficinaId = (SELECT OficinaId FROM CREDITO.Credito WHERE CreditoId = @CreditoId)
  AND a.Estado = CAST(1 AS bit);

/* ========== 2) Aplicar cola de remate (descomentar) ==========
BEGIN TRAN;

IF NOT EXISTS (
    SELECT 1
    FROM CREDITO.Credito AS c
    INNER JOIN MAESTRO.Persona AS pe ON pe.PersonaId = c.PersonaId
    WHERE c.CreditoId = @CreditoId
      AND pe.NumeroDocumento = @Dni
      AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
      AND c.FechaRemateEjecutado IS NULL
)
BEGIN
    ROLLBACK;
    THROW 50001, N'Abortado: no coincide 87695 + DNI 77403936 + prendario sin remate ejecutado.', 1;
END;

-- Almacén activo de la sede (si no hay, el USP de remate falla).
IF NOT EXISTS (
    SELECT 1
    FROM ALMACEN.Almacen AS a
    INNER JOIN CREDITO.Credito AS c ON c.OficinaId = a.OficinaId
    WHERE c.CreditoId = @CreditoId
      AND a.Estado = CAST(1 AS bit)
)
BEGIN
    INSERT INTO ALMACEN.Almacen (OficinaId, Denominacion, Descripcion, IndEstadoApertura, FechaApertura, Estado)
    SELECT c.OficinaId, N'ALMACEN LAB', N'Alta lab para probar remate 87695', CAST(1 AS bit), dbo.ufnFecha(), CAST(1 AS bit)
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;
END;

UPDATE c
SET
    Estado = N'DES',
    FechaVencimiento = DATEADD(DAY, -31, CONVERT(date, dbo.ufnFecha())),
    FechaRemate = DATEADD(DAY, -1, CONVERT(date, dbo.ufnFecha())),
    FechaRemateEjecutado = NULL,
    UsuarioModId = c.UsuarioModId,
    FechaMod = dbo.ufnFecha()
FROM CREDITO.Credito AS c
INNER JOIN MAESTRO.Persona AS pe ON pe.PersonaId = c.PersonaId
WHERE c.CreditoId = @CreditoId
  AND pe.NumeroDocumento = @Dni;

-- El bien sigue EN CUSTODIA. No tocar PlanPago ni caja.

SELECT
    c.CreditoId,
    c.Estado,
    c.FechaVencimiento,
    c.FechaRemate,
    CONVERT(date, dbo.ufnFecha()) AS HoyBd,
    CAST(CASE
        WHEN c.Estado = N'DES'
         AND c.FechaRemateEjecutado IS NULL
         AND COALESCE(c.FechaRemate, DATEADD(DAY, 30, c.FechaVencimiento)) < CONVERT(date, dbo.ufnFecha())
        THEN 1 ELSE 0 END AS bit) AS PuedeRematarAhora
FROM CREDITO.Credito AS c
WHERE c.CreditoId = @CreditoId;

-- COMMIT;     -- si PuedeRematarAhora = 1
-- ROLLBACK;
========== */

/* ========== 3) Revertir a APR + fechas originales (si hace falta) ==========
BEGIN TRAN;
UPDATE CREDITO.Credito
SET Estado = N'APR',
    FechaVencimiento = '20261115',
    FechaRemate = '20261215',
    FechaRemateEjecutado = NULL
WHERE CreditoId = 87695;
-- COMMIT;
========== */
