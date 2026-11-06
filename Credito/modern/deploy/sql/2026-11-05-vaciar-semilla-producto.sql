/*
  SEMILLA DE PRODUCTO (base genérica para entregar / vender)
  ----------------------------------------------------------
  SOLO Azure CREDITO de prueba (o laboratorio). NO producción con clientes reales.

  Corrige el error del wipe OPERATIVO anterior:
    - conservaba Usuario y truncaba Persona → huérfanos + FK is_not_trusted
    - recreaba FKs WITH NOCHECK

  Modos:
    SEMILLA   = catálogos + menú/roles + 1 oficina + 1 admin + 1 caja + bóveda en 0
                (sin clientes, créditos, movimientos, ni usuarios de prueba)
    OPERATIVO = deja usuarios/oficinas/cajas/productos; CONSERVA Persona (coherente)
    TOTAL     = vacía casi todo (sin semilla; solo esquema + lo que no liste)

  Cómo:
    1) Poner @Confirmar = N'BORRAR-AZURE-PRUEBA'
    2) @Modo = N'SEMILLA'  (recomendado para producto genérico)
    3) SSMS → timeout 0 → F5
    4) Login: ADMIN / Admin123!  (cambiar clave al entrar)

  Si el portal Azure corta a mitad: F5 de nuevo (reanuda vía dbo._CredixWipeFk).
*/

SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @Confirmar nvarchar(40) = N'CAMBIAR-ANTES-DE-EJECUTAR';
DECLARE @Modo      nvarchar(20) = N'SEMILLA';

IF @Confirmar <> N'BORRAR-AZURE-PRUEBA'
BEGIN
    RAISERROR(N'Abortado: pon @Confirmar = N''BORRAR-AZURE-PRUEBA''. Solo laboratorio/Azure prueba.', 16, 1);
    RETURN;
END;

IF @Modo NOT IN (N'SEMILLA', N'OPERATIVO', N'TOTAL')
BEGIN
    RAISERROR(N'Abortado: @Modo = SEMILLA | OPERATIVO | TOTAL.', 16, 1);
    RETURN;
END;

IF DB_NAME() IN (N'master', N'msdb', N'tempdb', N'model')
BEGIN
    RAISERROR(N'Abortado: base de sistema.', 16, 1);
    RETURN;
END;

PRINT N'Base: ' + DB_NAME();
PRINT N'Modo: ' + @Modo;
PRINT N'Servidor: ' + @@SERVERNAME;

DECLARE @Conservar TABLE (
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL,
    PRIMARY KEY (SchemaName, TableName)
);

/* Catálogos de producto (genéricos, no son datos de un cliente). */
INSERT INTO @Conservar (SchemaName, TableName) VALUES
    (N'MAESTRO', N'Menu'),
    (N'MAESTRO', N'Rol'),
    (N'MAESTRO', N'RolMenu'),
    (N'MAESTRO', N'TipoDocumento'),
    (N'MAESTRO', N'TipoOperacion'),
    (N'MAESTRO', N'ValorTabla'),
    (N'MAESTRO', N'Departamento'),
    (N'MAESTRO', N'Provincia'),
    (N'MAESTRO', N'Distrito'),
    (N'MAESTRO', N'Ocupacion'),
    (N'CREDITO', N'Producto'),
    (N'CREDITO', N'GastosAdm'),
    (N'CREDITO', N'PlaParametro'),
    (N'ALMACEN', N'TipoArticulo'),
    (N'ALMACEN', N'TipoMovimiento'),
    (N'ALMACEN', N'Marca'),
    (N'ALMACEN', N'Modelo');

IF @Modo = N'OPERATIVO'
BEGIN
    /* Coherencia: si quedan usuarios, DEBE quedar Persona (y Acceso si aplica). */
    INSERT INTO @Conservar (SchemaName, TableName) VALUES
        (N'MAESTRO', N'Persona'),
        (N'MAESTRO', N'Usuario'),
        (N'MAESTRO', N'UsuarioRol'),
        (N'MAESTRO', N'UsuarioOficina'),
        (N'MAESTRO', N'Oficina'),
        (N'MAESTRO', N'Acceso'),
        (N'CREDITO', N'Caja'),
        (N'CREDITO', N'Boveda'),
        (N'CREDITO', N'BovedaCuenta'),
        (N'ALMACEN', N'Almacen'),
        (N'ALMACEN', N'Articulo'),
        (N'VENTAS',  N'ListaPrecio');
END
/* SEMILLA / TOTAL: no conservan Persona/Usuario/Oficina/Caja/Boveda (SEMILLA siembra después). */

DECLARE @Tablas TABLE (
    SchemaName sysname NOT NULL,
    TableName  sysname NOT NULL,
    TwoPart    nvarchar(300) NOT NULL,
    PRIMARY KEY (SchemaName, TableName)
);

INSERT INTO @Tablas (SchemaName, TableName, TwoPart)
SELECT
    s.name,
    t.name,
    QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
  AND t.temporal_type = 0
  AND s.name IN (N'CREDITO', N'MAESTRO', N'ALMACEN', N'VENTAS', N'dbo')
  AND t.name NOT IN (N'sysdiagrams', N'dtproperties', N'_CredixWipeFk')
  AND NOT EXISTS (
        SELECT 1 FROM @Conservar AS k
        WHERE k.SchemaName = s.name AND k.TableName = t.name
  );

IF NOT EXISTS (SELECT 1 FROM @Tablas)
BEGIN
    RAISERROR(N'No hay tablas para vaciar.', 16, 1);
    RETURN;
END;

PRINT N'Tablas a vaciar: ' + CONVERT(varchar(12), (SELECT COUNT(*) FROM @Tablas));

IF OBJECT_ID(N'dbo._CredixWipeFk', N'U') IS NULL
BEGIN
    CREATE TABLE dbo._CredixWipeFk (
        FkId int IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        DropSql nvarchar(max) NOT NULL,
        CreateSql nvarchar(max) NOT NULL,
        Dropped bit NOT NULL CONSTRAINT DF_CredixWipeFk_Dropped2 DEFAULT (0),
        Recreated bit NOT NULL CONSTRAINT DF_CredixWipeFk_Recreated2 DEFAULT (0)
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo._CredixWipeFk)
BEGIN
    /* WITH CHECK: integridad real (no NOCHECK → is_not_trusted). */
    INSERT INTO dbo._CredixWipeFk (DropSql, CreateSql)
    SELECT
        N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id))
            + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
            + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';',
        N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id))
            + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
            + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(fk.name)
            + N' FOREIGN KEY (' + pc.ParentCols + N') REFERENCES '
            + QUOTENAME(OBJECT_SCHEMA_NAME(fk.referenced_object_id))
            + N'.' + QUOTENAME(OBJECT_NAME(fk.referenced_object_id))
            + N' (' + rc.RefCols + N')'
            + CASE fk.delete_referential_action
                WHEN 1 THEN N' ON DELETE CASCADE'
                WHEN 2 THEN N' ON DELETE SET NULL'
                WHEN 3 THEN N' ON DELETE SET DEFAULT'
                ELSE N''
              END
            + CASE fk.update_referential_action
                WHEN 1 THEN N' ON UPDATE CASCADE'
                WHEN 2 THEN N' ON UPDATE SET NULL'
                WHEN 3 THEN N' ON UPDATE SET DEFAULT'
                ELSE N''
              END
            + N';'
            + N' ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id))
            + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
            + N' CHECK CONSTRAINT ' + QUOTENAME(fk.name) + N';'
    FROM sys.foreign_keys AS fk
    CROSS APPLY (
        SELECT STRING_AGG(QUOTENAME(c.name), N',') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS ParentCols
        FROM sys.foreign_key_columns AS fkc
        INNER JOIN sys.columns AS c
            ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        WHERE fkc.constraint_object_id = fk.object_id
    ) AS pc
    CROSS APPLY (
        SELECT STRING_AGG(QUOTENAME(c.name), N',') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS RefCols
        FROM sys.foreign_key_columns AS fkc
        INNER JOIN sys.columns AS c
            ON c.object_id = fkc.referenced_object_id AND c.column_id = fkc.referenced_column_id
        WHERE fkc.constraint_object_id = fk.object_id
    ) AS rc
    WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) IN (N'CREDITO', N'MAESTRO', N'ALMACEN', N'VENTAS', N'dbo')
       OR OBJECT_SCHEMA_NAME(fk.referenced_object_id) IN (N'CREDITO', N'MAESTRO', N'ALMACEN', N'VENTAS', N'dbo');

    PRINT N'FKs respaldadas: ' + CONVERT(varchar(12), (SELECT COUNT(*) FROM dbo._CredixWipeFk));
END
ELSE
    PRINT N'Reanudando: dbo._CredixWipeFk ya tenía FKs.';

DECLARE @sql nvarchar(max);
DECLARE @TwoPart nvarchar(300);
DECLARE @FkId int;

DECLARE curDrop CURSOR LOCAL FAST_FORWARD FOR
    SELECT FkId, DropSql FROM dbo._CredixWipeFk WHERE Dropped = 0;
OPEN curDrop;
FETCH NEXT FROM curDrop INTO @FkId, @sql;
WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        EXEC (@sql);
        UPDATE dbo._CredixWipeFk SET Dropped = 1 WHERE FkId = @FkId;
    END TRY
    BEGIN CATCH
        PRINT N'FK drop omitido: ' + ERROR_MESSAGE();
        UPDATE dbo._CredixWipeFk SET Dropped = 1 WHERE FkId = @FkId;
    END CATCH
    FETCH NEXT FROM curDrop INTO @FkId, @sql;
END
CLOSE curDrop;
DEALLOCATE curDrop;

PRINT N'Truncando...';
DECLARE curTr CURSOR LOCAL FAST_FORWARD FOR SELECT TwoPart FROM @Tablas;
OPEN curTr;
FETCH NEXT FROM curTr INTO @TwoPart;
WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        SET @sql = N'TRUNCATE TABLE ' + @TwoPart + N';';
        EXEC (@sql);
        PRINT N'TRUNCATE ' + @TwoPart;
    END TRY
    BEGIN CATCH
        PRINT N'TRUNCATE falló ' + @TwoPart + N': ' + ERROR_MESSAGE() + N' → DELETE.';
        BEGIN TRY
            EXEC (N'DISABLE TRIGGER ALL ON ' + @TwoPart + N';');
        END TRY
        BEGIN CATCH
        END CATCH

        DECLARE @n int = 1;
        WHILE @n > 0
        BEGIN
            SET @sql = N'DELETE TOP (20000) FROM ' + @TwoPart + N';';
            EXEC (@sql);
            SET @n = @@ROWCOUNT;
        END

        IF EXISTS (
            SELECT 1
            FROM sys.identity_columns AS ic
            INNER JOIN sys.tables AS t ON t.object_id = ic.object_id
            INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
            WHERE QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) = @TwoPart
        )
        BEGIN
            BEGIN TRY
                EXEC (N'DBCC CHECKIDENT (''' + @TwoPart + N''', RESEED, 0) WITH NO_INFOMSGS;');
            END TRY
            BEGIN CATCH
            END CATCH
        END
    END CATCH
    FETCH NEXT FROM curTr INTO @TwoPart;
END
CLOSE curTr;
DEALLOCATE curTr;

IF @Modo = N'OPERATIVO'
BEGIN
    IF OBJECT_ID(N'CREDITO.BovedaCuenta', N'U') IS NOT NULL
        UPDATE CREDITO.BovedaCuenta SET SaldoInicial = 0 WHERE SaldoInicial <> 0;

    IF OBJECT_ID(N'CREDITO.Boveda', N'U') IS NOT NULL
    BEGIN
        UPDATE CREDITO.Boveda
        SET SaldoInicial = 0, Entradas = 0, Salidas = 0, SaldoFinal = 0
        WHERE IndCierre = CAST(0 AS bit);

        UPDATE CREDITO.Boveda
        SET IndCierre = CAST(1 AS bit),
            SaldoInicial = 0, Entradas = 0, Salidas = 0, SaldoFinal = 0
        WHERE IndTemporal = CAST(1 AS bit) AND IndCierre = CAST(0 AS bit);
    END
END

PRINT N'Recreando FKs WITH CHECK...';
DECLARE curAdd CURSOR LOCAL FAST_FORWARD FOR
    SELECT FkId, CreateSql FROM dbo._CredixWipeFk WHERE Recreated = 0;
OPEN curAdd;
FETCH NEXT FROM curAdd INTO @FkId, @sql;
WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        EXEC (@sql);
        UPDATE dbo._CredixWipeFk SET Recreated = 1 WHERE FkId = @FkId;
    END TRY
    BEGIN CATCH
        PRINT N'FK recreate omitida: ' + ERROR_MESSAGE();
        UPDATE dbo._CredixWipeFk SET Recreated = 1 WHERE FkId = @FkId;
    END CATCH
    FETCH NEXT FROM curAdd INTO @FkId, @sql;
END
CLOSE curAdd;
DEALLOCATE curAdd;

IF NOT EXISTS (SELECT 1 FROM dbo._CredixWipeFk WHERE Recreated = 0)
    DROP TABLE dbo._CredixWipeFk;

/* -------- Semilla de producto -------- */
IF @Modo = N'SEMILLA'
BEGIN
    PRINT N'Sembrando oficina / admin / caja / bóveda...';

    DECLARE @PersonaId int;
    DECLARE @UsuarioId int;
    DECLARE @OficinaId int;
    DECLARE @RolId int =
    (
        SELECT TOP (1) r.RolId
        FROM MAESTRO.Rol AS r
        WHERE r.Estado = CAST(1 AS bit)
          AND (
                UPPER(r.Denominacion) LIKE N'%ADMIN%'
             OR UPPER(r.Denominacion) LIKE N'%MAESTRO%'
             OR UPPER(r.Denominacion) = N'SUPERVISOR'
          )
        ORDER BY r.RolId
    );

    IF @RolId IS NULL
        SET @RolId = (SELECT TOP (1) RolId FROM MAESTRO.Rol WHERE Estado = CAST(1 AS bit) ORDER BY RolId);

    IF @RolId IS NULL
    BEGIN
        RAISERROR(N'SEMILLA: no hay roles. Conserve MAESTRO.Rol o cárguelos antes.', 16, 1);
        RETURN;
    END;

    INSERT INTO MAESTRO.Persona (
        Nombre, ApePaterno, ApeMaterno, NombreCompleto,
        TipoDocumento, NumeroDocumento, Sexo, TipoPersona, Estado, Celular1)
    VALUES (
        N'ADMIN', N'SISTEMA', N'', N'ADMIN SISTEMA',
        N'DNI', N'00000001', N'M', N'N', CAST(1 AS bit), N'999999999');

    SET @PersonaId = CONVERT(int, SCOPE_IDENTITY());

    /* Clave plana inicial; el login moderno la migra a hash si la columna lo permite. */
    INSERT INTO MAESTRO.Usuario (PersonaId, NombreUsuario, ClaveUsuario, Estado)
    VALUES (@PersonaId, N'ADMIN', N'Admin123!', CAST(1 AS bit));

    SET @UsuarioId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO MAESTRO.Oficina (Denominacion, Descripcion, IndPrincipal, Estado, UsuarioAsignadoId)
    VALUES (N'OFICINA PRINCIPAL', N'Sede semilla del producto', CAST(1 AS bit), CAST(1 AS bit), @UsuarioId);

    SET @OficinaId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
    VALUES (@UsuarioId, @OficinaId, CAST(1 AS bit));

    INSERT INTO MAESTRO.UsuarioRol (UsuarioId, RolId, OficinaId)
    VALUES (@UsuarioId, @RolId, @OficinaId);

    IF OBJECT_ID(N'ALMACEN.Almacen', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM ALMACEN.Almacen)
    BEGIN
        /* Columnas mínimas varían; solo si hay OficinaId/Denominacion típicos. */
        IF COL_LENGTH(N'ALMACEN.Almacen', N'Denominacion') IS NOT NULL
           AND COL_LENGTH(N'ALMACEN.Almacen', N'OficinaId') IS NOT NULL
        BEGIN
            BEGIN TRY
                INSERT INTO ALMACEN.Almacen (OficinaId, Denominacion, Estado)
                VALUES (@OficinaId, N'ALMACEN PRINCIPAL', CAST(1 AS bit));
            END TRY
            BEGIN CATCH
                PRINT N'Almacen semilla omitido: ' + ERROR_MESSAGE();
            END CATCH
        END
    END

    INSERT INTO CREDITO.Caja (OficinaId, Denominacion, Estado, UsuarioRegId, FechaReg, IndAbierto)
    VALUES (@OficinaId, N'CAJA PRINCIPAL', CAST(1 AS bit), @UsuarioId, SYSUTCDATETIME(), CAST(0 AS bit));

    INSERT INTO CREDITO.Boveda (
        OficinaId, SaldoInicial, Entradas, Salidas, SaldoFinal,
        FechaIniOperacion, IndCierre, IndTemporal)
    VALUES (
        @OficinaId, 0, 0, 0, 0,
        SYSUTCDATETIME(), CAST(0 AS bit), CAST(0 AS bit));

    IF OBJECT_ID(N'CREDITO.BovedaCuenta', N'U') IS NOT NULL
    BEGIN
        DECLARE @BovedaId int = (SELECT TOP (1) BovedaId FROM CREDITO.Boveda WHERE OficinaId = @OficinaId AND IndCierre = 0 ORDER BY BovedaId);
        IF @BovedaId IS NOT NULL
           AND COL_LENGTH(N'CREDITO.BovedaCuenta', N'TipoPagoId') IS NOT NULL
        BEGIN
            BEGIN TRY
                INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
                SELECT @BovedaId, vt.ItemId, 0
                FROM MAESTRO.ValorTabla AS vt
                WHERE vt.TablaId = 13 AND vt.ItemId BETWEEN 1 AND 5
                  AND NOT EXISTS (
                        SELECT 1 FROM CREDITO.BovedaCuenta AS bc
                        WHERE bc.BovedaId = @BovedaId AND bc.TipoPagoId = vt.ItemId);
            END TRY
            BEGIN CATCH
                PRINT N'BovedaCuenta semilla omitida: ' + ERROR_MESSAGE();
            END CATCH
        END
    END

    PRINT N'Semilla OK.';
    PRINT N'Login: ADMIN / Admin123!  (cambiar clave).';
    PRINT N'OficinaId=' + CONVERT(varchar(12), @OficinaId)
        + N' UsuarioId=' + CONVERT(varchar(12), @UsuarioId)
        + N' RolId=' + CONVERT(varchar(12), @RolId);
END

PRINT N'=== Integridad Usuario→Persona ===';
SELECT
    SUM(CASE WHEN p.PersonaId IS NULL THEN 1 ELSE 0 END) AS Huerfanos,
    COUNT(*) AS Usuarios
FROM MAESTRO.Usuario AS u
LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId;

SELECT fk.name AS ForeignKey, fk.is_disabled, fk.is_not_trusted
FROM sys.foreign_keys AS fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = N'MAESTRO'
  AND OBJECT_NAME(fk.parent_object_id) = N'Usuario'
  AND OBJECT_NAME(fk.referenced_object_id) = N'Persona';

PRINT N'Listo. Modo ' + @Modo + N' en ' + DB_NAME() + N'.';
PRINT N'SEMILLA = producto genérico (sin clientes ni historial de prueba).';
GO
