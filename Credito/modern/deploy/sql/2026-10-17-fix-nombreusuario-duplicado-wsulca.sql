-- =============================================================================
-- Login roto: dos MAESTRO.Usuario activos con NombreUsuario = WSULCA (#1038 y #1039)
-- En ese caso el login moderno fallaba (ambigüedad) con mensaje engañoso de
-- "configuración incompleta". Debe quedar UN solo activo con ese login.
-- =============================================================================
SET NOCOUNT ON;

-- 1) Diagnóstico
SELECT
    u.UsuarioId,
    u.NombreUsuario,
    u.Estado AS UsuarioActivo,
    p.NombreCompleto,
    uo.OficinaId,
    o.Denominacion AS Oficina,
    uo.Estado AS OficinaActiva
FROM MAESTRO.Usuario AS u
INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
LEFT JOIN MAESTRO.UsuarioOficina AS uo ON uo.UsuarioId = u.UsuarioId
LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = uo.OficinaId
WHERE UPPER(LTRIM(RTRIM(u.NombreUsuario))) = N'WSULCA'
ORDER BY u.UsuarioId, uo.OficinaId;

-- 2) Corrección recomendada (prod actual):
--    #1039 = persona real (WASINTHON VICTOR) → mantener WSULCA
--    #1038 = cuenta E2E / duplicado → renombrar y dejar inactiva si no se usa
-- Descomentar tras revisar el SELECT:

/*
BEGIN TRAN;

UPDATE MAESTRO.Usuario
SET NombreUsuario = N'CREDITO_E2E_USER',
    Estado = CAST(0 AS bit)
WHERE UsuarioId = 1038
  AND UPPER(LTRIM(RTRIM(NombreUsuario))) = N'WSULCA';

-- Verificar que solo queda un WSULCA activo
SELECT UsuarioId, NombreUsuario, Estado
FROM MAESTRO.Usuario
WHERE UPPER(LTRIM(RTRIM(NombreUsuario))) IN (N'WSULCA', N'CREDITO_E2E_USER');

-- COMMIT;
-- ROLLBACK;
*/
