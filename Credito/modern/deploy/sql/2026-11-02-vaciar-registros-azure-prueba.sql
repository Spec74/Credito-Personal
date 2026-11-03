/*
  VACÍA REGISTROS. No borra tablas ni procedimientos.

  SOLO Azure CREDITO de prueba. NO ejecutar en producción.

  El editor del portal corta a ~5 min. Este script:
    - no usa una transacción gigante (si se corta, F5 otra vez y sigue)
    - suelta FKs, TRUNCATE (rápido), las vuelve a crear
    - guarda el SQL de las FKs en dbo._CredixWipeFk por si el portal cae a mitad

  Cómo: Azure de prueba → F5. Si sale "severe error", vuelve a F5 el mismo archivo.
  Preferible: SSMS contra Azure, timeout de consulta = 0.

  OPERATIVO = borra operación, deja usuarios/menú/oficinas/productos/cajas.
*/

SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @Confirmar nvarchar(40) = N'BORRAR-AZURE-PRUEBA';
DECLARE @Modo      nvarchar(20) = N'OPERATIVO';

IF @Confirmar <> N'BORRAR-AZURE-PRUEBA'
BEGIN
    RAISERROR(N'Abortado: pon @Confirmar = N''BORRAR-AZURE-PRUEBA''. Solo Azure de prueba.', 16, 1);
    RETURN;
END;

IF @Modo NOT IN (N'OPERATIVO', N'TOTAL')
BEGIN
    RAISERROR(N'Abortado: @Modo debe ser OPERATIVO o TOTAL.', 16, 1);
    RETURN;
END;

IF DB_NAME() IN (N'master', N'msdb', N'tempdb', N'model')
BEGIN
    RAISERROR(N'Abortado: no se puede ejecutar en bases de sistema.', 16, 1);
    RETURN;
END;

PRINT N'Base: ' + DB_NAME();
PRINT N'Modo: ' + @Modo;
PRINT N'Servidor: ' + @@SERVERNAME;

DECLARE @Conservar TABLE (SchemaName sysname NOT NULL, TableName sysname NOT NULL, PRIMARY KEY (SchemaName, TableName));

IF @Modo = N'OPERATIVO'
BEGIN
    INSERT INTO @Conservar (SchemaName, TableName) VALUES
        (N'MAESTRO', N'Menu'),
        (N'MAESTRO', N'Rol'),
        (N'MAESTRO', N'RolMenu'),
        (N'MAESTRO', N'Usuario'),
        (N'MAESTRO', N'UsuarioRol'),
        (N'MAESTRO', N'UsuarioOficina'),
        (N'MAESTRO', N'Oficina'),
        (N'MAESTRO', N'Departamento'),
        (N'MAESTRO', N'Provincia'),
        (N'MAESTRO', N'Distrito'),
        (N'MAESTRO', N'Ocupacion'),
        (N'MAESTRO', N'TipoDocumento'),
        (N'MAESTRO', N'TipoOperacion'),
        (N'MAESTRO', N'ValorTabla'),
        (N'CREDITO', N'Producto'),
        (N'CREDITO', N'GastosAdm'),
        (N'CREDITO', N'PlaParametro'),
        (N'CREDITO', N'Caja'),
        (N'CREDITO', N'Boveda'),
        (N'CREDITO', N'BovedaCuenta'),
        (N'ALMACEN', N'Almacen'),
        (N'ALMACEN', N'TipoArticulo'),
        (N'ALMACEN', N'TipoMovimiento'),
        (N'ALMACEN', N'Marca'),
        (N'ALMACEN', N'Modelo'),
        (N'ALMACEN', N'Articulo'),
        (N'VENTAS',  N'ListaPrecio');
END;

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
        SELECT 1
        FROM @Conservar AS k
        WHERE k.SchemaName = s.name
          AND k.TableName = t.name
  );

IF NOT EXISTS (SELECT 1 FROM @Tablas)
BEGIN
    RAISERROR(N'No hay tablas para borrar.', 16, 1);
    RETURN;
END;

SELECT
    x.SchemaName,
    x.TableName,
    SUM(p.rows) AS FilasAprox
FROM @Tablas AS x
INNER JOIN sys.tables AS t ON t.name = x.TableName
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id AND s.name = x.SchemaName
INNER JOIN sys.partitions AS p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
GROUP BY x.SchemaName, x.TableName
ORDER BY x.SchemaName, x.TableName;

DECLARE @nTablas int = (SELECT COUNT(*) FROM @Tablas);
PRINT N'Tablas a vaciar: ' + CONVERT(varchar(12), @nTablas);

IF OBJECT_ID(N'dbo._CredixWipeFk', N'U') IS NULL
BEGIN
    CREATE TABLE dbo._CredixWipeFk (
        FkId int IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        DropSql nvarchar(max) NOT NULL,
        CreateSql nvarchar(max) NOT NULL,
        Dropped bit NOT NULL CONSTRAINT DF_CredixWipeFk_Dropped DEFAULT (0),
        Recreated bit NOT NULL CONSTRAINT DF_CredixWipeFk_Recreated DEFAULT (0)
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo._CredixWipeFk)
BEGIN
    INSERT INTO dbo._CredixWipeFk (DropSql, CreateSql)
    SELECT
        N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id))
            + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
            + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';',
        N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id))
            + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
            + N' WITH NOCHECK ADD CONSTRAINT ' + QUOTENAME(fk.name)
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

    DECLARE @nFk int = (SELECT COUNT(*) FROM dbo._CredixWipeFk);
    PRINT N'FKs respaldadas: ' + CONVERT(varchar(12), @nFk);
END
ELSE
    PRINT N'Reanudando: dbo._CredixWipeFk ya tenia FKs.';

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

PRINT N'FKs sueltas. Truncando...';

DECLARE curTr CURSOR LOCAL FAST_FORWARD FOR
    SELECT TwoPart FROM @Tablas;
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
        PRINT N'TRUNCATE falló en ' + @TwoPart + N': ' + ERROR_MESSAGE() + N' → DELETE por lotes.';
        BEGIN TRY
            SET @sql = N'DISABLE TRIGGER ALL ON ' + @TwoPart + N';';
            EXEC (@sql);
        END TRY
        BEGIN CATCH
            PRINT N'Trigger disable omitido en ' + @TwoPart;
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
            SET @sql = N'DBCC CHECKIDENT (''' + @TwoPart + N''', RESEED, 0) WITH NO_INFOMSGS;';
            BEGIN TRY
                EXEC (@sql);
            END TRY
            BEGIN CATCH
                PRINT N'CHECKIDENT omitido en ' + @TwoPart;
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
       AND COL_LENGTH(N'CREDITO.BovedaCuenta', N'SaldoInicial') IS NOT NULL
    BEGIN
        UPDATE CREDITO.BovedaCuenta
        SET SaldoInicial = 0
        WHERE SaldoInicial <> 0;
        PRINT N'BovedaCuenta: SaldoInicial en 0.';
    END

    -- Cabecera Boveda se conserva (abierta), pero SI/E/S/SF eran del ledger viejo.
    IF OBJECT_ID(N'CREDITO.Boveda', N'U') IS NOT NULL
    BEGIN
        UPDATE CREDITO.Boveda
        SET SaldoInicial = 0,
            Entradas = 0,
            Salidas = 0,
            SaldoFinal = 0
        WHERE IndCierre = CAST(0 AS bit)
          AND (
                SaldoInicial <> 0
             OR Entradas <> 0
             OR Salidas <> 0
             OR SaldoFinal <> 0
          );
        PRINT N'Boveda abierta: cabecera SI/E/S/SF en 0.';

        UPDATE CREDITO.Boveda
        SET IndCierre = CAST(1 AS bit),
            SaldoInicial = 0,
            Entradas = 0,
            Salidas = 0,
            SaldoFinal = 0
        WHERE IndTemporal = CAST(1 AS bit)
          AND IndCierre = CAST(0 AS bit);
        PRINT N'Boveda temporal abierta: cerrada.';
    END
END

PRINT N'Recreando FKs...';

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

SELECT
    x.SchemaName,
    x.TableName,
    SUM(p.rows) AS FilasDespues
FROM @Tablas AS x
INNER JOIN sys.tables AS t ON t.name = x.TableName
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id AND s.name = x.SchemaName
INNER JOIN sys.partitions AS p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
GROUP BY x.SchemaName, x.TableName
ORDER BY x.SchemaName, x.TableName;

PRINT N'Listo. Modo ' + @Modo + N' en ' + DB_NAME() + N'.';
PRINT N'Siguen intactos: menu, roles, usuarios, oficinas, productos, gastos, cajas, almacenes, ubigeo.';
GO
