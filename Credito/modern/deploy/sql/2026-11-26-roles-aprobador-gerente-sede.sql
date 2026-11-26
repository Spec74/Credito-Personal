-- Roles multi-sucursal: APROBADOR unificado + tipado ENCARGADO (gerente de sede).
-- Idempotente. No borra filas legacy APROBADOR 1/2 (quedan inactivas tras migrar asignaciones).

SET NOCOUNT ON;
GO

/* 1) Asegurar rol APROBADOR activo */
IF NOT EXISTS (
    SELECT 1 FROM MAESTRO.Rol
    WHERE REPLACE(UPPER(LTRIM(RTRIM(Denominacion))), ' ', '') = N'APROBADOR'
)
BEGIN
    INSERT INTO MAESTRO.Rol (Denominacion, Estado)
    VALUES (N'APROBADOR', CAST(1 AS bit));
END
ELSE
BEGIN
    UPDATE MAESTRO.Rol
    SET Estado = CAST(1 AS bit)
    WHERE REPLACE(UPPER(LTRIM(RTRIM(Denominacion))), ' ', '') = N'APROBADOR';
END
GO

DECLARE @AprobadorId INT = (
    SELECT TOP (1) RolId
    FROM MAESTRO.Rol
    WHERE REPLACE(UPPER(LTRIM(RTRIM(Denominacion))), ' ', '') = N'APROBADOR'
    ORDER BY RolId
);

DECLARE @Legacy TABLE (RolId INT PRIMARY KEY);
INSERT INTO @Legacy (RolId)
SELECT RolId
FROM MAESTRO.Rol
WHERE REPLACE(UPPER(LTRIM(RTRIM(Denominacion))), ' ', '') IN (
    N'APROBADOR1', N'APROBADOR2', N'APROBADOR01', N'APROBADOR02'
);

/* 2) Migrar UsuarioRol 1/2 → APROBADOR (sin duplicar) */
IF @AprobadorId IS NOT NULL AND EXISTS (SELECT 1 FROM @Legacy)
BEGIN
    INSERT INTO MAESTRO.UsuarioRol (UsuarioId, OficinaId, RolId)
    SELECT DISTINCT ur.UsuarioId, ur.OficinaId, @AprobadorId
    FROM MAESTRO.UsuarioRol AS ur
    INNER JOIN @Legacy AS l ON l.RolId = ur.RolId
    WHERE NOT EXISTS (
        SELECT 1
        FROM MAESTRO.UsuarioRol AS x
        WHERE x.UsuarioId = ur.UsuarioId
          AND x.OficinaId = ur.OficinaId
          AND x.RolId = @AprobadorId
    );

    DELETE ur
    FROM MAESTRO.UsuarioRol AS ur
    INNER JOIN @Legacy AS l ON l.RolId = ur.RolId;

    UPDATE r
    SET r.Estado = CAST(0 AS bit)
    FROM MAESTRO.Rol AS r
    INNER JOIN @Legacy AS l ON l.RolId = r.RolId;
END
GO

/* 3) ENCARGADO activo (gerente de sede) — no crear si ya existe */
IF NOT EXISTS (
    SELECT 1 FROM MAESTRO.Rol
    WHERE REPLACE(UPPER(LTRIM(RTRIM(Denominacion))), ' ', '') = N'ENCARGADO'
)
BEGIN
    INSERT INTO MAESTRO.Rol (Denominacion, Estado)
    VALUES (N'ENCARGADO', CAST(1 AS bit));
END
ELSE
BEGIN
    UPDATE MAESTRO.Rol
    SET Estado = CAST(1 AS bit)
    WHERE REPLACE(UPPER(LTRIM(RTRIM(Denominacion))), ' ', '') = N'ENCARGADO';
END
GO
