/*
  Reparar FK Usuario → Persona (Azure / CREDITO)
  ----------------------------------------------
  Si is_not_trusted = 1: la FK existe pero SQL Server NO garantiza integridad
  (se creó WITH NOCHECK o hubo violaciones). Pasos:

  1) Listar huérfanos
  2) Reparar (Administración → Usuarios, o UPDATE PersonaId correcto)
  3) Revalidar FK WITH CHECK
  4) Confirmar is_not_trusted = 0

  NO ejecutar el ALTER hasta Huerfanos = 0.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT '=== 1. Estado actual de la FK ===';
SELECT
    fk.name AS ForeignKey,
    fk.is_disabled,
    fk.is_not_trusted
FROM sys.foreign_keys AS fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = N'MAESTRO'
  AND OBJECT_NAME(fk.parent_object_id) = N'Usuario'
  AND OBJECT_NAME(fk.referenced_object_id) = N'Persona';

PRINT '=== 2. Usuarios huérfanos (rompen la FK) ===';
SELECT
    u.UsuarioId,
    u.NombreUsuario,
    u.PersonaId,
    u.Estado AS UsuarioActivo,
    CASE
        WHEN u.PersonaId IS NULL OR u.PersonaId <= 0 THEN N'SIN_PERSONA_ID'
        ELSE N'PERSONA_NO_EXISTE'
    END AS Diagnostico
FROM MAESTRO.Usuario AS u
LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
WHERE p.PersonaId IS NULL
ORDER BY u.UsuarioId;

DECLARE @Huerfanos int =
(
    SELECT COUNT(1)
    FROM MAESTRO.Usuario AS u
    LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
    WHERE p.PersonaId IS NULL
);

PRINT CONCAT('Huérfanos: ', @Huerfanos);

IF @Huerfanos > 0
BEGIN
    PRINT 'DETENER: corrija los usuarios listados (Administración → Usuarios / UPDATE PersonaId).';
    PRINT 'No se revalida la FK mientras haya huérfanos.';
    RETURN;
END

PRINT '=== 3. Revalidar FK (WITH CHECK) ===';
BEGIN TRY
    ALTER TABLE MAESTRO.Usuario
        WITH CHECK
        CHECK CONSTRAINT FK__Usuario__Persona__33C07256;

    PRINT 'FK revalidada OK.';
END TRY
BEGIN CATCH
    PRINT CONCAT('Error al revalidar: ', ERROR_MESSAGE());
    PRINT 'Si el nombre de FK difiere, use el de la consulta del paso 1.';
    THROW;
END CATCH

PRINT '=== 4. Confirmar confianza ===';
SELECT
    fk.name AS ForeignKey,
    fk.is_disabled,
    fk.is_not_trusted
FROM sys.foreign_keys AS fk
WHERE fk.name = N'FK__Usuario__Persona__33C07256';

/*
  Esperado:
  is_disabled = 0
  is_not_trusted = 0
*/
