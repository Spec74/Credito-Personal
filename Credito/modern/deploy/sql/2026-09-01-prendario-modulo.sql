-- Modulo de credito prendario solicitado por gerencia.
--
-- Sustituye a la tabla provisional CREDITO.CreditoPrenda del sistema moderno: se adopta el
-- nombre y el diseno CREDITO.Prenda para que el MVC legacy y la SPA moderna lean la misma
-- tabla durante la migracion strangler.
--
-- Correcciones aplicadas sobre el script original de gerencia, ver
-- docs/migration/BITACORA-DESVIACIONES.md:
--   1. Faltaba la coma antes de CONSTRAINT FK_Prenda_Credito: el CREATE TABLE no compilaba.
--   2. Credito.PrendaId era INT contra Prenda.PrendaId BIGINT IDENTITY.
--   3. Los RolMenu apuntaban a los MenuId 38 y 39, que ya existen (CONDONACION y DASHBOARD):
--      habrian dado al analista acceso a esos dos modulos en lugar de a prendario. Aqui los
--      MenuId se resuelven por denominacion.
--   4. Se agregan columnas de auditoria: una prenda es un bien fisico en custodia y debe
--      constar quien la registro y quien la modifico.
--
-- Idempotente: se reaplica tras cada restauracion de base del cliente.

-- =============================================
-- 1. Columnas de prendario en CREDITO.Credito
-- =============================================

IF COL_LENGTH(N'CREDITO.Credito', N'EsPrendario') IS NULL
BEGIN
    ALTER TABLE CREDITO.Credito
        ADD EsPrendario bit NOT NULL CONSTRAINT DF_Credito_EsPrendario DEFAULT (0);
END
GO

IF COL_LENGTH(N'CREDITO.Credito', N'MontoTasacion') IS NULL
BEGIN
    ALTER TABLE CREDITO.Credito ADD MontoTasacion decimal(18,2) NULL;
END
GO

IF COL_LENGTH(N'CREDITO.Credito', N'NumeroContratoPrendario') IS NULL
BEGIN
    ALTER TABLE CREDITO.Credito ADD NumeroContratoPrendario nvarchar(50) NULL;
END
GO

IF COL_LENGTH(N'CREDITO.Credito', N'FechaRemate') IS NULL
BEGIN
    ALTER TABLE CREDITO.Credito ADD FechaRemate date NULL;
END
GO

-- Denormalizacion heredada del script de gerencia: apunta a la prenda principal cuando el
-- credito tiene varias. Sin clave foranea para no crear una dependencia circular con Prenda.
IF COL_LENGTH(N'CREDITO.Credito', N'PrendaId') IS NULL
BEGIN
    ALTER TABLE CREDITO.Credito ADD PrendaId bigint NULL;
END
GO

-- =============================================
-- 2. Tabla CREDITO.Prenda
-- =============================================

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
        Estado nvarchar(50) NOT NULL CONSTRAINT DF_Prenda_Estado DEFAULT ('EN CUSTODIA'),
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

-- =============================================
-- 3. Relleno del indicador en la cartera existente
-- =============================================
-- EsPrendario nace en 0 para todo, incluidos los creditos del producto CREDI PRENDARIO que ya
-- existian. Sin este relleno el resumen del modulo marcaria cero creditos prendarios vigentes
-- aunque la cartera los tenga, y el indice filtrado IX_Credito_EsPrendario quedaria vacio.
-- El producto es la fuente de verdad: ProductoId = 2 es CREDI PRENDARIO.

UPDATE CREDITO.Credito
SET EsPrendario = CAST(1 AS bit)
WHERE ProductoId = 2
  AND EsPrendario = CAST(0 AS bit);
GO

-- =============================================
-- 4. Indices
-- =============================================

-- Indice filtrado: exige QUOTED_IDENTIFIER ON en todo modulo que haga UPDATE/INSERT
-- sobre CREDITO.Credito. Ver 2026-09-03-usp-credito-ins-quoted-identifier.sql.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Credito_EsPrendario' AND object_id = OBJECT_ID(N'CREDITO.Credito'))
BEGIN
    CREATE INDEX IX_Credito_EsPrendario ON CREDITO.Credito (EsPrendario) WHERE EsPrendario = 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Prenda_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.Prenda'))
BEGIN
    CREATE INDEX IX_Prenda_CreditoId ON CREDITO.Prenda (CreditoId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Prenda_Estado' AND object_id = OBJECT_ID(N'CREDITO.Prenda'))
BEGIN
    CREATE INDEX IX_Prenda_Estado ON CREDITO.Prenda (Estado);
END
GO

-- =============================================
-- 5. Menu
-- =============================================
-- usp_MenuLst deriva el menu padre uniendo Menu.Orden con Referencia de los hijos concedidos,
-- por eso el padre no lleva fila en RolMenu.

IF NOT EXISTS (SELECT 1 FROM MAESTRO.Menu WHERE Denominacion = 'CREDI PRENDARIO')
BEGIN
    INSERT INTO MAESTRO.Menu (Denominacion, Modulo, Url, Icono, IndPadre, Orden, Referencia)
    VALUES ('CREDI PRENDARIO', '', NULL, 'img/icons/packs/fugue/16x16/box.png', 1, 70.0, NULL);
END
GO

IF NOT EXISTS (SELECT 1 FROM MAESTRO.Menu WHERE Denominacion = 'PRENDARIO - Listado')
BEGIN
    INSERT INTO MAESTRO.Menu (Denominacion, Modulo, Url, Icono, IndPadre, Orden, Referencia)
    VALUES ('PRENDARIO - Listado', 'PRENDARIO', 'Prendario', 'icon-list', 0, 70.1, 70.0);
END
GO

IF NOT EXISTS (SELECT 1 FROM MAESTRO.Menu WHERE Denominacion = 'PRENDARIO - Nuevo')
BEGIN
    INSERT INTO MAESTRO.Menu (Denominacion, Modulo, Url, Icono, IndPadre, Orden, Referencia)
    VALUES ('PRENDARIO - Nuevo', 'PRENDARIO', 'Prendario/Create', 'icon-list', 0, 70.2, 70.0);
END
GO

-- =============================================
-- 6. Permisos: solo el rol ANALISTA
-- =============================================

INSERT INTO MAESTRO.RolMenu (RolId, MenuId)
SELECT r.RolId, m.MenuId
FROM MAESTRO.Rol AS r
CROSS JOIN MAESTRO.Menu AS m
WHERE r.Denominacion = 'ANALISTA'
  AND m.Denominacion IN ('PRENDARIO - Listado', 'PRENDARIO - Nuevo')
  AND NOT EXISTS (
      SELECT 1 FROM MAESTRO.RolMenu AS rm
      WHERE rm.RolId = r.RolId AND rm.MenuId = m.MenuId);
GO

