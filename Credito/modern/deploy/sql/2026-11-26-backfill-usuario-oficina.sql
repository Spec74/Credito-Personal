/*
  Repara vínculos sede ↔ usuario cuando hay roles, caja o UsuarioAsignadoId
  de oficina pero falta MAESTRO.UsuarioOficina.

  Sin ese vínculo, Seguridad → Usuarios puede salir vacío aunque
  Mantenimiento → Cajas muestre el gestor (CajeroId).

  Idempotente. El API aplica la misma lógica en UsuarioOficinaVinculoSql
  (EnsureFromEvidenceAsync al listar; UpsertAsync al guardar caja/usuario).
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

INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
SELECT DISTINCT o.UsuarioAsignadoId, o.OficinaId, CAST(1 AS bit)
FROM MAESTRO.Oficina AS o
INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = o.UsuarioAsignadoId
WHERE o.UsuarioAsignadoId IS NOT NULL
  AND o.UsuarioAsignadoId >= 1
  AND o.OficinaId >= 1
  AND NOT EXISTS (
      SELECT 1
      FROM MAESTRO.UsuarioOficina AS uo
      WHERE uo.UsuarioId = o.UsuarioAsignadoId
        AND uo.OficinaId = o.OficinaId);
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
        OR EXISTS (
            SELECT 1
            FROM MAESTRO.Oficina AS o
            WHERE o.UsuarioAsignadoId = uo.UsuarioId
              AND o.OficinaId = uo.OficinaId)
      );
GO

SELECT
    (SELECT COUNT(*) FROM MAESTRO.Usuario) AS Usuarios,
    (SELECT COUNT(*) FROM MAESTRO.UsuarioOficina) AS UsuarioOficina,
    (SELECT COUNT(*) FROM MAESTRO.UsuarioOficina WHERE Estado = CAST(1 AS bit)) AS UsuarioOficinaActivos;
GO
