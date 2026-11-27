/*
  Repara vínculos sede ↔ usuario cuando hay roles o caja asignada pero falta
  MAESTRO.UsuarioOficina. Sin ese vínculo, Seguridad → Usuarios sale vacío
  aunque Mantenimiento → Cajas muestre el gestor (CajeroId).

  Idempotente. Ejecutar en Azure y local tras los scripts 2026-11-25/26.
*/

SET NOCOUNT ON;
GO

INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
SELECT DISTINCT ur.UsuarioId, ur.OficinaId, CAST(1 AS bit)
FROM MAESTRO.UsuarioRol AS ur
INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = ur.UsuarioId
WHERE ur.OficinaId >= 1
  AND NOT EXISTS (
      SELECT 1
      FROM MAESTRO.UsuarioOficina AS uo
      WHERE uo.UsuarioId = ur.UsuarioId
        AND uo.OficinaId = ur.OficinaId);
GO

INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
SELECT DISTINCT c.CajeroId, c.OficinaId, CAST(1 AS bit)
FROM CREDITO.Caja AS c
INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.CajeroId
WHERE c.CajeroId >= 1
  AND c.OficinaId >= 1
  AND NOT EXISTS (
      SELECT 1
      FROM MAESTRO.UsuarioOficina AS uo
      WHERE uo.UsuarioId = c.CajeroId
        AND uo.OficinaId = c.OficinaId);
GO

UPDATE uo
SET uo.Estado = CAST(1 AS bit)
FROM MAESTRO.UsuarioOficina AS uo
WHERE uo.Estado = CAST(0 AS bit)
  AND (
        EXISTS (
            SELECT 1
            FROM MAESTRO.UsuarioRol AS ur
            WHERE ur.UsuarioId = uo.UsuarioId
              AND ur.OficinaId = uo.OficinaId)
        OR EXISTS (
            SELECT 1
            FROM CREDITO.Caja AS c
            WHERE c.CajeroId = uo.UsuarioId
              AND c.OficinaId = uo.OficinaId)
      );
GO

SELECT
    (SELECT COUNT(*) FROM MAESTRO.Usuario) AS Usuarios,
    (SELECT COUNT(*) FROM MAESTRO.UsuarioOficina) AS UsuarioOficina,
    (SELECT COUNT(*) FROM MAESTRO.UsuarioOficina WHERE Estado = CAST(1 AS bit)) AS UsuarioOficinaActivos;
GO
