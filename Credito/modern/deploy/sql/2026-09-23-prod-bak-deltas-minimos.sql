/*
================================================================================
  Deltas mínimos sobre BAK de producción (CREDITO20260922)
================================================================================
  Verificado en localhost,14330 / CREDITO (backup 2026-09-22):
    - YA EXISTEN: CreditoCondonacion, CierreGerencial, usp_Dashboard*,
      usp_SolicitarCondonacion, usp_RegistrarTransferenciaBancos, etc.
    - FALTAN SOLO: Geo, Prendario (+ menú), ClaveUsuario nvarchar(256).

  NO ejecutar el script grande 2026-09-23-azure-cutover-modern-deltas.sql
  sobre esta base: es redundante (objetos de negocio ya vienen en el bak).

  Uso Azure:
    1) Restaurar CREDITO20260922.bak en Azure (reemplaza la DB vieja).
    2) Ejecutar ESTE archivo.
    3) Revisar el SELECT final.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- Geo
IF COL_LENGTH(N'MAESTRO.Cliente', N'Latitud') IS NULL
    ALTER TABLE MAESTRO.Cliente ADD Latitud decimal(11,8) NULL;
IF COL_LENGTH(N'MAESTRO.Cliente', N'Longitud') IS NULL
    ALTER TABLE MAESTRO.Cliente ADD Longitud decimal(11,8) NULL;
IF COL_LENGTH(N'MAESTRO.Oficina', N'Latitud') IS NULL
    ALTER TABLE MAESTRO.Oficina ADD Latitud decimal(11,8) NULL;
IF COL_LENGTH(N'MAESTRO.Oficina', N'Longitud') IS NULL
    ALTER TABLE MAESTRO.Oficina ADD Longitud decimal(11,8) NULL;
GO

-- Credito (prendario)
IF COL_LENGTH(N'CREDITO.Credito', N'EsPrendario') IS NULL
    ALTER TABLE CREDITO.Credito ADD EsPrendario bit NOT NULL CONSTRAINT DF_Credito_EsPrendario DEFAULT (0);
IF COL_LENGTH(N'CREDITO.Credito', N'MontoTasacion') IS NULL
    ALTER TABLE CREDITO.Credito ADD MontoTasacion decimal(18,2) NULL;
IF COL_LENGTH(N'CREDITO.Credito', N'NumeroContratoPrendario') IS NULL
    ALTER TABLE CREDITO.Credito ADD NumeroContratoPrendario nvarchar(50) NULL;
IF COL_LENGTH(N'CREDITO.Credito', N'FechaRemate') IS NULL
    ALTER TABLE CREDITO.Credito ADD FechaRemate date NULL;
IF COL_LENGTH(N'CREDITO.Credito', N'PrendaId') IS NULL
    ALTER TABLE CREDITO.Credito ADD PrendaId bigint NULL;
IF COL_LENGTH(N'CREDITO.Credito', N'FechaNotifWhatsapp3d') IS NULL
    ALTER TABLE CREDITO.Credito ADD FechaNotifWhatsapp3d datetime NULL;
GO

UPDATE CREDITO.Credito
SET EsPrendario = 1
WHERE ProductoId = 2 AND EsPrendario = 0;
GO

-- Prenda (coma antes del FK)
IF OBJECT_ID(N'CREDITO.Prenda', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.Prenda (
        PrendaId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Prenda PRIMARY KEY,
        CreditoId int NOT NULL,
        Descripcion nvarchar(500) NOT NULL,
        Marca nvarchar(100) NULL,
        Modelo nvarchar(100) NULL,
        Serie nvarchar(100) NULL,
        Color nvarchar(50) NULL,
        ValorTasacion decimal(18,2) NOT NULL,
        Observaciones nvarchar(max) NULL,
        FotoPath nvarchar(500) NULL,
        Estado nvarchar(50) NOT NULL CONSTRAINT DF_Prenda_Estado DEFAULT (N'EN CUSTODIA'),
        FechaRegistro datetime NOT NULL CONSTRAINT DF_Prenda_FechaRegistro DEFAULT (GETDATE()),
        CodigoInterno nvarchar(50) NULL,
        UsuarioRegId int NULL,
        UsuarioModId int NULL,
        FechaMod datetime NULL,
        CONSTRAINT FK_Prenda_Credito FOREIGN KEY (CreditoId)
            REFERENCES CREDITO.Credito (CreditoId)
    );
END
GO

SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Credito_EsPrendario' AND object_id = OBJECT_ID(N'CREDITO.Credito'))
    CREATE INDEX IX_Credito_EsPrendario ON CREDITO.Credito (EsPrendario) WHERE EsPrendario = 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Prenda_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.Prenda'))
    CREATE INDEX IX_Prenda_CreditoId ON CREDITO.Prenda (CreditoId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Prenda_Estado' AND object_id = OBJECT_ID(N'CREDITO.Prenda'))
    CREATE INDEX IX_Prenda_Estado ON CREDITO.Prenda (Estado);
GO

-- Menú (sin MenuId 38/39)
IF NOT EXISTS (SELECT 1 FROM MAESTRO.Menu WHERE Denominacion = N'CREDI PRENDARIO')
    INSERT INTO MAESTRO.Menu (Denominacion, Modulo, Url, Icono, IndPadre, Orden, Referencia)
    VALUES (N'CREDI PRENDARIO', N'', NULL, N'img/icons/packs/fugue/16x16/box.png', 1, 70.0, NULL);

IF NOT EXISTS (SELECT 1 FROM MAESTRO.Menu WHERE Denominacion = N'PRENDARIO - Listado')
    INSERT INTO MAESTRO.Menu (Denominacion, Modulo, Url, Icono, IndPadre, Orden, Referencia)
    VALUES (N'PRENDARIO - Listado', N'PRENDARIO', N'Prendario', N'icon-list', 0, 70.1, 70.0);

IF NOT EXISTS (SELECT 1 FROM MAESTRO.Menu WHERE Denominacion = N'PRENDARIO - Nuevo')
    INSERT INTO MAESTRO.Menu (Denominacion, Modulo, Url, Icono, IndPadre, Orden, Referencia)
    VALUES (N'PRENDARIO - Nuevo', N'PRENDARIO', N'Prendario/Create', N'icon-list', 0, 70.2, 70.0);
GO

INSERT INTO MAESTRO.RolMenu (RolId, MenuId)
SELECT r.RolId, m.MenuId
FROM MAESTRO.Rol AS r
CROSS JOIN MAESTRO.Menu AS m
WHERE r.Denominacion = N'ANALISTA'
  AND m.Denominacion IN (N'PRENDARIO - Listado', N'PRENDARIO - Nuevo')
  AND NOT EXISTS (
      SELECT 1 FROM MAESTRO.RolMenu AS rm
      WHERE rm.RolId = r.RolId AND rm.MenuId = m.MenuId);
GO

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

-- Verificación
SELECT
    COL_LENGTH(N'MAESTRO.Cliente', N'Latitud') AS ClienteLatitud,
    COL_LENGTH(N'CREDITO.Credito', N'EsPrendario') AS EsPrendario,
    OBJECT_ID(N'CREDITO.Prenda', N'U') AS Prenda,
    (SELECT COUNT(*) FROM MAESTRO.Menu WHERE Denominacion LIKE N'PRENDARIO%') AS MenusPrendario,
    (
        SELECT CHARACTER_MAXIMUM_LENGTH
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = N'MAESTRO' AND TABLE_NAME = N'Usuario' AND COLUMN_NAME = N'ClaveUsuario'
    ) AS ClaveUsuarioLen,
    (SELECT COUNT(*) FROM CREDITO.Credito WHERE EsPrendario = 1) AS CreditosPrendariosMarcados;
GO

PRINT N'Deltas mínimos (geo + prendario + clave) OK.';
GO
