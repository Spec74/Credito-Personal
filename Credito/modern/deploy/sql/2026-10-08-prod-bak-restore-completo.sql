/*
================================================================================
  RESTORE COMPLETO tras un .bak de producción (tipo CREDITO20260922 / 2026-10)
================================================================================
  Un solo archivo. Idempotente. Correr en la base CREDITO después de restaurar.

  Incluye:
    A) Geo + Prendario (columnas, Prenda, índices, menú ANALISTA) + ClaveUsuario 256
    B) Mora postergada (MovimientoCajaId NULL + usp_CreditoMora_*)
    C) usp_Credito_Ins con QUOTED_IDENTIFIER ON (evita error 1934 al generar crédito)

  NO ejecutar:
    - 2026-09-23-azure-cutover-modern-deltas.sql (redundante en estos bak)
    - No agregar FechaNotifWhatsapp3d

  Opcional después (solo si falta):
    - 2026-10-07-usp-morosidad-empresa.sql  (en bak 2026-09-22/10-06 suele venir)
    - ALTER usp_PagarCuotas Extension       (API moderna ya tiene safety-net C#)

  Uso:
    1) Restaurar .bak en Azure (reemplaza la DB).
    2) Ejecutar ESTE archivo completo.
    3) Revisar los SELECT / PRINT finales.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* -------------------------------------------------------------------------- */
/* A1. Geo                                                                    */
/* -------------------------------------------------------------------------- */
IF COL_LENGTH(N'MAESTRO.Cliente', N'Latitud') IS NULL
    ALTER TABLE MAESTRO.Cliente ADD Latitud decimal(11,8) NULL;
IF COL_LENGTH(N'MAESTRO.Cliente', N'Longitud') IS NULL
    ALTER TABLE MAESTRO.Cliente ADD Longitud decimal(11,8) NULL;
IF COL_LENGTH(N'MAESTRO.Oficina', N'Latitud') IS NULL
    ALTER TABLE MAESTRO.Oficina ADD Latitud decimal(11,8) NULL;
IF COL_LENGTH(N'MAESTRO.Oficina', N'Longitud') IS NULL
    ALTER TABLE MAESTRO.Oficina ADD Longitud decimal(11,8) NULL;
GO

/* -------------------------------------------------------------------------- */
/* A2. Credito (prendario) — GO obligatorio antes del UPDATE                   */
/* -------------------------------------------------------------------------- */
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
GO

UPDATE CREDITO.Credito
SET EsPrendario = 1
WHERE ProductoId = 2 AND EsPrendario = 0;
GO

/* -------------------------------------------------------------------------- */
/* A3. Tabla Prenda                                                           */
/* -------------------------------------------------------------------------- */
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

/* -------------------------------------------------------------------------- */
/* A4. Menú prendario + RolMenu ANALISTA                                      */
/* -------------------------------------------------------------------------- */
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

/* -------------------------------------------------------------------------- */
/* A5. ClaveUsuario nvarchar(256) para hash PBKDF2                            */
/* -------------------------------------------------------------------------- */
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

PRINT N'A) Deltas mínimos (geo + prendario + clave) OK.';
GO

/* -------------------------------------------------------------------------- */
/* B. Mora postergada                                                         */
/* -------------------------------------------------------------------------- */
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'CREDITO.CreditoMora')
      AND name = N'MovimientoCajaId'
      AND is_nullable = 0)
BEGIN
    ALTER TABLE CREDITO.CreditoMora ALTER COLUMN MovimientoCajaId int NULL;
END
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_CreditoMora_Registrar]
    @CreditoId INT,
    @FechaVencimiento DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FechaActual DATE = dbo.ufnFecha();

    DECLARE @tCuotasPendientes TABLE(
        Id INT IDENTITY(1,1), PlanPagoId INT, Glosa VARCHAR(MAX), FechaVencimiento DATE,
        Amortizacion DECIMAL(16,2), Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2),
        Cuota DECIMAL(16,2), DiasAtrazo INT, ImporteMora DECIMAL(16,2),
        Descuento DECIMAL(16,2), Cargo DECIMAL(16,2), PagoLibre DECIMAL(16,2), PagoCuota DECIMAL(16,2)
    );

    INSERT INTO @tCuotasPendientes
    EXEC CREDITO.usp_CuotasPendientes @CreditoId, @FechaActual;

    IF EXISTS (SELECT 1 FROM @tCuotasPendientes WHERE FechaVencimiento = @FechaVencimiento AND DiasAtrazo > 0)
    BEGIN
        DECLARE @DiasAtrazo REAL, @MontoMora DECIMAL(16,2);

        SELECT
            @DiasAtrazo = DiasAtrazo,
            @MontoMora = ImporteMora
        FROM @tCuotasPendientes
        WHERE FechaVencimiento = @FechaVencimiento;

        IF @MontoMora > 0 AND NOT EXISTS (SELECT 1 FROM [CREDITO].[CreditoMora] WHERE CreditoId = @CreditoId AND MovimientoCajaId IS NULL)
        BEGIN
            INSERT INTO [CREDITO].[CreditoMora] (
                [CreditoId], [MovimientoCajaId], [Fecha], [Mora], [DiasAtrazo], [SaldoMora], [InteresMora]
            )
            VALUES (
                @CreditoId, NULL, GETDATE(), @MontoMora, @DiasAtrazo, @MontoMora, 0.00
            );
        END
    END
END
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_CreditoMora_Liquidar]
    @CreditoId INT,
    @CajaDiarioId INT,
    @UsuarioId INT,
    @TipoPagoId INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalMoraAPagar DECIMAL(16,2) =
        (SELECT ISNULL(SUM(SaldoMora), 0) FROM [CREDITO].[CreditoMora] WHERE [CreditoId] = @CreditoId AND [MovimientoCajaId] IS NULL);

    IF @TotalMoraAPagar > 0
    BEGIN
        DECLARE @PersonaId INT, @MovimientoCajaId INT;
        SELECT @PersonaId = PersonaId FROM CREDITO.Credito WHERE CreditoId = @CreditoId;

        INSERT INTO CREDITO.MovimientoCaja (
            CajaDiarioId, PersonaId, Operacion, ImportePago, Descripcion,
            IndEntrada, Estado, OrdenVentaId, CreditoId, UsuarioRegId, FechaReg, TipoPagoId
        )
        VALUES (
            @CajaDiarioId, @PersonaId, 'MOR', @TotalMoraAPagar,
            'CREDITO ' + CAST(@CreditoId AS VARCHAR(20)) + ' LIQUIDACION DE MORAS',
            1, 1, NULL, @CreditoId, @UsuarioId, GETDATE(), @TipoPagoId
        );

        SET @MovimientoCajaId = CAST(SCOPE_IDENTITY() AS INT);

        UPDATE [CREDITO].[CreditoMora]
        SET
            [MovimientoCajaId] = @MovimientoCajaId,
            [SaldoMora] = 0.00
        WHERE
            [CreditoId] = @CreditoId
            AND [MovimientoCajaId] IS NULL;

        EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId;

        SELECT @MovimientoCajaId AS MovimientoCajaId;
    END
    ELSE
    BEGIN
        SELECT 0 AS MovimientoCajaId;
    END
END
GO

PRINT N'B) Mora postergada OK.';
GO

/* -------------------------------------------------------------------------- */
/* C. usp_Credito_Ins → QUOTED_IDENTIFIER ON (sin cambiar el cuerpo)          */
/* -------------------------------------------------------------------------- */
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'CREDITO.usp_Credito_Ins', N'P') IS NULL
BEGIN
    THROW 51000, 'CREDITO.usp_Credito_Ins no existe.', 1;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.sql_modules
    WHERE object_id = OBJECT_ID(N'CREDITO.usp_Credito_Ins')
      AND uses_quoted_identifier = 0
)
BEGIN
    DECLARE @def nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'CREDITO.usp_Credito_Ins'));
    IF @def IS NULL
    BEGIN
        THROW 51000, 'No se pudo leer OBJECT_DEFINITION de CREDITO.usp_Credito_Ins.', 1;
    END;

    DECLARE @pos int = PATINDEX(N'%PROC%', @def);
    IF @pos < 1
    BEGIN
        THROW 51000, 'OBJECT_DEFINITION de usp_Credito_Ins no contiene PROC.', 1;
    END;

    SET @def = N'CREATE OR ALTER ' + SUBSTRING(@def, @pos, LEN(@def));
    EXEC sys.sp_executesql @def;
END
GO

PRINT N'C) usp_Credito_Ins QUOTED_IDENTIFIER OK.';
GO

/* -------------------------------------------------------------------------- */
/* Verificación final                                                         */
/* -------------------------------------------------------------------------- */
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
    (SELECT COUNT(*) FROM CREDITO.Credito WHERE EsPrendario = 1) AS CreditosPrendariosMarcados,
    (
        SELECT uses_quoted_identifier
        FROM sys.sql_modules
        WHERE object_id = OBJECT_ID(N'CREDITO.usp_Credito_Ins')
    ) AS CreditoInsQuotedIdentifierOn,
    OBJECT_ID(N'CREDITO.usp_CreditoMora_Registrar', N'P') AS MoraRegistrar,
    OBJECT_ID(N'CREDITO.usp_MorosidadEmpresa', N'P') AS MorosidadEmpresa;
GO

PRINT N'Restore completo (A+B+C) OK. Si MorosidadEmpresa es NULL, aplicar 2026-10-07-usp-morosidad-empresa.sql.';
GO
