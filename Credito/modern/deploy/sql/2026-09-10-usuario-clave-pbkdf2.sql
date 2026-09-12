-- Amplia MAESTRO.Usuario.ClaveUsuario para el hash PBKDF2 ($pbk2$, ~85 caracteres).
-- No modifica claves existentes. Hace falta antes de Auth:MigracionClavePerezosa=true
-- o de crear/resetear usuarios desde la API moderna (ya escribe hash).
-- Idempotente: no toca la columna si ya tiene longitud >= 256.

IF COL_LENGTH(N'MAESTRO.Usuario', N'ClaveUsuario') IS NOT NULL
BEGIN
    DECLARE @maxLen int =
    (
        SELECT CHARACTER_MAXIMUM_LENGTH
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = N'MAESTRO'
          AND TABLE_NAME = N'Usuario'
          AND COLUMN_NAME = N'ClaveUsuario'
    );

    IF @maxLen IS NOT NULL AND @maxLen > 0 AND @maxLen < 256
    BEGIN
        ALTER TABLE MAESTRO.Usuario
            ALTER COLUMN ClaveUsuario nvarchar(256) NOT NULL;
    END
END
GO
