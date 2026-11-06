/*
  Auditoría multisucursal: Usuario → Persona
  ------------------------------------------
  Caja y bóveda exigen PersonaId del operador en MovimientoCaja (paridad MVC).
  Un Usuario.PersonaId huérfano rompe FK_MovimientoCaja_Persona.

  Ejecutar en Azure/local (CREDITO). No modifica datos: solo lista.
  Reparación: Administración → Usuarios (guardar ficha crea/relinkea Persona por DNI)
  o UPDATE manual de MAESTRO.Usuario.PersonaId al PersonaId correcto.
*/

SET NOCOUNT ON;

PRINT '=== Usuarios activos sin persona válida ===';
SELECT
    u.UsuarioId,
    u.NombreUsuario,
    u.PersonaId,
    u.Estado AS UsuarioActivo,
    CASE
        WHEN u.PersonaId IS NULL OR u.PersonaId <= 0 THEN N'SIN_PERSONA_ID'
        WHEN p.PersonaId IS NULL THEN N'HUERFANO_NO_EXISTE_EN_PERSONA'
        ELSE N'OK'
    END AS Diagnostico
FROM MAESTRO.Usuario AS u
LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
WHERE u.Estado = CAST(1 AS bit)
  AND (
        u.PersonaId IS NULL
        OR u.PersonaId <= 0
        OR p.PersonaId IS NULL
      )
ORDER BY Diagnostico, u.NombreUsuario;

PRINT '=== Oficinas donde esos usuarios tienen UsuarioOficina activo ===';
SELECT
    u.UsuarioId,
    u.NombreUsuario,
    o.OficinaId,
    o.Denominacion AS Oficina,
    uo.UsuarioOficinaId
FROM MAESTRO.Usuario AS u
INNER JOIN MAESTRO.UsuarioOficina AS uo ON uo.UsuarioId = u.UsuarioId AND uo.Estado = CAST(1 AS bit)
INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = uo.OficinaId
LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
WHERE u.Estado = CAST(1 AS bit)
  AND (
        u.PersonaId IS NULL
        OR u.PersonaId <= 0
        OR p.PersonaId IS NULL
      )
ORDER BY o.Denominacion, u.NombreUsuario;

PRINT '=== FK Usuario→Persona (debe existir y estar trusted) ===';
SELECT
    fk.name AS ForeignKey,
    fk.is_disabled,
    fk.is_not_trusted
FROM sys.foreign_keys AS fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = N'MAESTRO'
  AND OBJECT_NAME(fk.parent_object_id) = N'Usuario'
  AND COL_NAME(
        (SELECT parent_object_id FROM sys.foreign_key_columns WHERE constraint_object_id = fk.object_id AND constraint_column_id = 1),
        (SELECT parent_column_id FROM sys.foreign_key_columns WHERE constraint_object_id = fk.object_id AND constraint_column_id = 1)
      ) = N'PersonaId'
   OR (
        OBJECT_SCHEMA_NAME(fk.parent_object_id) = N'MAESTRO'
        AND OBJECT_NAME(fk.parent_object_id) = N'Usuario'
        AND OBJECT_NAME(fk.referenced_object_id) = N'Persona'
      );

/*
  Si is_not_trusted = 1, tras reparar huérfanos:

  ALTER TABLE MAESTRO.Usuario WITH CHECK CHECK CONSTRAINT FK__Usuario__Persona__...;

  (usar el nombre real de la FK listado arriba)
*/
