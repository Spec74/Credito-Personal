-- SOLO BD local de desarrollo. NO aplicar en Azure.
-- NO renombrar oficina 3: en producción es SUCURSAL AYACUCCHO.
-- Huanta es oficina nueva (Azure = 10). Usar 2026-10-24-alinear-sedes-azure.sql.

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

-- NO renombrar oficina 3 a HUANTA. Eso mezcló Ayacucho con Huanta.
UPDATE MAESTRO.Oficina
SET Estado = CAST(1 AS bit),
    IndPrincipal = CAST(0 AS bit)
WHERE OficinaId = 3;

IF NOT EXISTS (
    SELECT 1
    FROM MAESTRO.Oficina
    WHERE Denominacion LIKE N'%PICHARI%'
)
BEGIN
    INSERT INTO MAESTRO.Oficina (
        Denominacion, Descripcion, Telefono, IndPrincipal, Estado, UsuarioAsignadoId)
    VALUES (N'PICHARI', N'Sucursal Pichari', NULL, CAST(0 AS bit), CAST(1 AS bit), 3);
END;

DECLARE @HuantaId INT = (
    SELECT TOP 1 OficinaId
    FROM MAESTRO.Oficina
    WHERE Denominacion LIKE N'%HUANTA%'
    ORDER BY OficinaId
);
DECLARE @PichariId INT = (
    SELECT TOP 1 OficinaId
    FROM MAESTRO.Oficina
    WHERE Denominacion LIKE N'%PICHARI%'
    ORDER BY OficinaId
);

IF @HuantaId IS NULL OR @PichariId IS NULL
BEGIN
    ROLLBACK;
    RAISERROR(N'No se resolvieron HUANTA y PICHARI.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM CREDITO.Boveda WHERE OficinaId = @HuantaId AND IndCierre = CAST(0 AS bit)
)
BEGIN
    INSERT INTO CREDITO.Boveda (
        OficinaId, SaldoInicial, Entradas, Salidas, SaldoFinal,
        FechaIniOperacion, IndCierre, IndTemporal)
    VALUES (
        @HuantaId, 0, 0, 0, 0,
        CAST(GETDATE() AS date), CAST(0 AS bit), CAST(0 AS bit));
END;

IF NOT EXISTS (
    SELECT 1 FROM CREDITO.Boveda WHERE OficinaId = @PichariId AND IndCierre = CAST(0 AS bit)
)
BEGIN
    INSERT INTO CREDITO.Boveda (
        OficinaId, SaldoInicial, Entradas, Salidas, SaldoFinal,
        FechaIniOperacion, IndCierre, IndTemporal)
    VALUES (
        @PichariId, 0, 0, 0, 0,
        CAST(GETDATE() AS date), CAST(0 AS bit), CAST(0 AS bit));
END;

;WITH Sedes AS (
    SELECT OficinaId FROM (VALUES (@HuantaId), (@PichariId)) AS s(OficinaId)
),
Usuarios AS (
    SELECT UsuarioId FROM MAESTRO.Usuario
    WHERE NombreUsuario IN (N'RMANTILLA', N'YCERVANTES') AND Estado = CAST(1 AS bit)
)
INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
SELECT u.UsuarioId, s.OficinaId, CAST(1 AS bit)
FROM Usuarios AS u
CROSS JOIN Sedes AS s
WHERE NOT EXISTS (
    SELECT 1
    FROM MAESTRO.UsuarioOficina AS uo
    WHERE uo.UsuarioId = u.UsuarioId
      AND uo.OficinaId = s.OficinaId
);

;WITH Sedes AS (
    SELECT OficinaId FROM (VALUES (@HuantaId), (@PichariId)) AS s(OficinaId)
)
INSERT INTO MAESTRO.UsuarioRol (UsuarioId, RolId, OficinaId)
SELECT ur.UsuarioId, ur.RolId, s.OficinaId
FROM MAESTRO.UsuarioRol AS ur
INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = ur.UsuarioId
CROSS JOIN Sedes AS s
WHERE u.NombreUsuario IN (N'RMANTILLA', N'YCERVANTES')
  AND ur.OficinaId = 1
  AND NOT EXISTS (
      SELECT 1
      FROM MAESTRO.UsuarioRol AS x
      WHERE x.UsuarioId = ur.UsuarioId
        AND x.RolId = ur.RolId
        AND x.OficinaId = s.OficinaId
  );

COMMIT;

SELECT
    o.OficinaId,
    o.Denominacion,
    o.Estado,
    o.IndPrincipal,
    (SELECT COUNT(*) FROM CREDITO.Boveda b WHERE b.OficinaId = o.OficinaId AND b.IndCierre = 0) AS BovedasAbiertas
FROM MAESTRO.Oficina AS o
WHERE o.OficinaId IN (1, @HuantaId, @PichariId)
ORDER BY o.OficinaId;
