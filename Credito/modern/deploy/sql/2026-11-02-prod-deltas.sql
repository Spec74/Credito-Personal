/*
================================================================================
  PAQUETE ÚNICO — deltas modernos sobre la BD CREDITO
  2026-11-02  (geo/prendario 2026-06 … domingo→lunes 2026-10-30)
================================================================================

  Qué hace
    Tablas, columnas, índices, menú y CREATE OR ALTER de procedimientos
    que el código moderno necesita y que el bak / prod de oct-2026 no trae
    (o trae viejo). No borra datos operativos. No vacía tablas.

  Dónde
    1) Azure CREDITO de prueba (F5 primero, revisar result sets).
    2) La base de producción que nos pasen, cuando el F5 de prueba esté OK.

  Cómo
    SSMS / Azure Query Editor → conectado a esa base (no master) → F5.
    Primer result set = qué falta ANTES. Último = qué quedó DESPUÉS.
    @Confirmar debe ser N'APLICAR-PROD-DELTAS' (ya está puesto).

  No incluye (a propósito)
    vaciar-registros-azure-prueba, lab y local, rename wsulca,
    boveda-huanta-sede (datos de sede), smoke/PITR de solo lectura,
    2026-09-23-azure-cutover-modern-deltas.sql (redundante y peligroso
    sobre bak de prod).

  Efecto de datos (acotado)
    - Marca EsPrendario=1 donde ProductoId=2 y aún está en 0.
    - Rellena FechaRemate = vencimiento+30 en prendarios DES sin fecha.
    - Crea ALMACEN.Almacen activo por oficina que no tenga uno.
    - Quita del menú Caja chica y Entrada de almacén (gerencia).
    - Crea menú VENTA RAPIDA si no existe.

  Domingo → lunes va AL FINAL a propósito: pisa el cuerpo de
  usp_SimuladorCredito / usp_Credito_Ins de prod (30/10/2026).
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'Base: ' + DB_NAME();
PRINT N'Servidor: ' + @@SERVERNAME;
PRINT N'Inicio paquete prod-deltas 2026-11-02';

IF DB_NAME() IN (N'master', N'msdb', N'tempdb', N'model')
BEGIN
    RAISERROR(N'Abortado: no se puede ejecutar en bases de sistema. Elige la base CREDITO.', 16, 1);
    RETURN;
END;

DECLARE @Confirmar nvarchar(40) = N'APLICAR-PROD-DELTAS';
IF @Confirmar <> N'APLICAR-PROD-DELTAS'
BEGIN
    RAISERROR(N'Abortado: pon @Confirmar = N''APLICAR-PROD-DELTAS''.', 16, 1);
    RETURN;
END;

SELECT
    DB_NAME() AS Base,
    @@SERVERNAME AS Servidor,
    CASE WHEN COL_LENGTH(N'MAESTRO.Cliente', N'Latitud') IS NULL THEN 0 ELSE 1 END AS ClienteLatitud,
    CASE WHEN COL_LENGTH(N'CREDITO.Credito', N'EsPrendario') IS NULL THEN 0 ELSE 1 END AS EsPrendario,
    CASE WHEN COL_LENGTH(N'CREDITO.Credito', N'FechaRemateEjecutado') IS NULL THEN 0 ELSE 1 END AS FechaRemateEjecutado,
    CASE WHEN COL_LENGTH(N'CREDITO.Prenda', N'ArticuloRemateId') IS NULL THEN 0 ELSE 1 END AS ArticuloRemateId,
    CASE WHEN OBJECT_ID(N'CREDITO.Prenda', N'U') IS NULL THEN 0 ELSE 1 END AS TablaPrenda,
    CASE WHEN OBJECT_ID(N'CREDITO.PlaParametro', N'U') IS NULL THEN 0 ELSE 1 END AS TablaPlaParametro,
    CASE WHEN OBJECT_ID(N'MAESTRO.Comunicado', N'U') IS NULL THEN 0 ELSE 1 END AS TablaComunicado,
    CASE WHEN OBJECT_ID(N'CREDITO.usp_DevolverPrenda', N'P') IS NULL THEN 0 ELSE 1 END AS usp_DevolverPrenda,
    CASE WHEN OBJECT_ID(N'CREDITO.usp_RematarPrenda', N'P') IS NULL THEN 0 ELSE 1 END AS usp_RematarPrenda,
    CASE WHEN OBJECT_ID(N'CREDITO.usp_PrendarioRenovar', N'P') IS NULL THEN 0 ELSE 1 END AS usp_PrendarioRenovar,
    CASE WHEN OBJECT_ID(N'CREDITO.usp_PrendarioLiberar', N'P') IS NULL THEN 0 ELSE 1 END AS usp_PrendarioLiberar,
    CASE WHEN OBJECT_ID(N'MAESTRO.usp_ComunicadoListar', N'P') IS NULL THEN 0 ELSE 1 END AS usp_ComunicadoListar,
    CASE WHEN OBJECT_ID(N'CREDITO.usp_MorosidadEmpresa', N'P') IS NULL THEN 0 ELSE 1 END AS usp_MorosidadEmpresa,
    CASE WHEN CHARINDEX(N'@SaltarDomingo', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'CREDITO.usp_SimuladorCredito')), N'')) > 0 THEN 1 ELSE 0 END AS SimuladorSaltarDomingo
    -- 0 = falta (el paquete lo crea/altera). 1 = ya estaba.
;
GO

GO
/* ===== INICIO 2026-10-08-prod-bak-restore-completo.sql ===== */
GO
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
GO
PRINT N'OK seccion 2026-10-08-prod-bak-restore-completo.sql';
GO

GO
/* ===== INICIO 2026-10-07-usp-morosidad-empresa.sql ===== */
GO
-- Morosidad empresarial (modulo /morosidad).
-- Aplicar en Azure SQL Database (base CREDITO) antes de usar el informe.
-- CREATE OR ALTER: sirve si el procedimiento aun no existe.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_MorosidadEmpresa]
(
    @Tipo        VARCHAR(30) = NULL,
    @OficinaId   INT = NULL,
    @UsuarioId   INT = NULL,
    @FechaCorte  DATE = NULL
)
WITH RECOMPILE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Hoy DATE = ISNULL(@FechaCorte, dbo.ufnFecha());
    DECLARE @Manana DATE = DATEADD(DAY, 1, @Hoy);

    SET @Tipo = UPPER(LTRIM(RTRIM(ISNULL(@Tipo, 'TODOS'))));

    IF @Tipo = ''
        SET @Tipo = 'TODOS';

    IF @Tipo NOT IN
    (
        'TODOS',
        'SIN_PAGO',
        'NUNCA_PAGO',
        'DEJO_PAGAR',
        'PAGA_CON_ATRASO'
    )
    BEGIN
        THROW 50001, 'El tipo de morosidad solicitado no es valido.', 1;
    END;

    IF @OficinaId IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM MAESTRO.Oficina O
           WHERE O.OficinaId = @OficinaId
       )
    BEGIN
        THROW 50002, 'La oficina solicitada no existe.', 1;
    END;

    IF @UsuarioId IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM MAESTRO.Usuario U
           WHERE U.UsuarioId = @UsuarioId
       )
    BEGIN
        THROW 50003, 'El gestor solicitado no existe.', 1;
    END;

    ;WITH CreditosActivos AS
    (
        SELECT
            C.CreditoId,
            C.PersonaId,
            C.OficinaId,
            C.UsuarioRegId AS GestorId
        FROM CREDITO.Credito C
        WHERE C.Estado = 'DES'
          AND ISNULL(C.IndIrrecuperable, 0) = 0
          AND C.PersonaId IS NOT NULL
          AND C.FechaDesembolso < @Manana
          AND (@OficinaId IS NULL OR C.OficinaId = @OficinaId)
          AND (@UsuarioId IS NULL OR C.UsuarioRegId = @UsuarioId)
    ),
    PlanAgregado AS
    (
        SELECT
            PP.CreditoId,
            SUM(ISNULL(PP.Cuota, 0) + ISNULL(PP.Cargo, 0)) AS MontoPlan,
            MAX
            (
                CASE
                    WHEN PP.Estado = 'PEN'
                     AND PP.FechaVencimiento < @Hoy
                    THEN 1 ELSE 0
                END
            ) AS TieneMora,
            MIN
            (
                CASE
                    WHEN PP.Estado = 'PEN'
                     AND PP.FechaVencimiento < @Hoy
                    THEN PP.FechaVencimiento
                END
            ) AS PrimeraCuotaVencida
        FROM CREDITO.PlanPago PP
        INNER JOIN CreditosActivos CA
            ON CA.CreditoId = PP.CreditoId
        GROUP BY PP.CreditoId
    ),
    PagosAgregados AS
    (
        SELECT
            M.CreditoId,
            SUM(M.ImportePago) AS TotalPagado,
            MAX(M.FechaReg) AS FechaUltimoPago
        FROM CREDITO.MovimientoCaja M
        INNER JOIN CreditosActivos CA
            ON CA.CreditoId = M.CreditoId
        WHERE M.Operacion = 'CUO'
          AND M.Estado = 1
          AND M.ImportePago > 0
          AND M.FechaReg < @Manana
        GROUP BY M.CreditoId
    ),
    CarteraCredito AS
    (
        SELECT
            CA.CreditoId,
            CA.PersonaId,
            CA.OficinaId,
            CA.GestorId,
            ISNULL(PA.TieneMora, 0) AS TieneMora,
            PA.PrimeraCuotaVencida,
            CAST
            (
                CASE
                    WHEN ISNULL(PA.MontoPlan, 0) - ISNULL(PG.TotalPagado, 0) > 0
                    THEN ISNULL(PA.MontoPlan, 0) - ISNULL(PG.TotalPagado, 0)
                    ELSE 0
                END
                AS DECIMAL(18, 2)
            ) AS Saldo,
            CAST(ISNULL(PG.TotalPagado, 0) AS DECIMAL(18, 2)) AS TotalPagado,
            PG.FechaUltimoPago
        FROM CreditosActivos CA
        LEFT JOIN PlanAgregado PA
            ON PA.CreditoId = CA.CreditoId
        LEFT JOIN PagosAgregados PG
            ON PG.CreditoId = CA.CreditoId
    ),
    CarteraPersona AS
    (
        SELECT
            PersonaId,
            COUNT(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN 1 END) AS CreditosMora,
            SUM(CASE WHEN TieneMora = 1 THEN Saldo ELSE 0 END) AS SaldoMora,
            MIN(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN PrimeraCuotaVencida END)
                AS PrimeraCuotaVencida,
            MAX(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN FechaUltimoPago END)
                AS FechaUltimoPago,
            SUM(CASE WHEN TieneMora = 1 THEN TotalPagado ELSE 0 END)
                AS PagadoEnCreditosMora,
            MAX
            (
                CASE
                    WHEN TieneMora = 1
                     AND Saldo > 0
                     AND FechaUltimoPago >= PrimeraCuotaVencida
                    THEN 1 ELSE 0
                END
            ) AS PagoDesdeInicioMora,
            MIN(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN OficinaId END)
                AS OficinaIdMin,
            MAX(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN OficinaId END)
                AS OficinaIdMax,
            MIN(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN GestorId END)
                AS GestorIdMin,
            MAX(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN GestorId END)
                AS GestorIdMax
        FROM CarteraCredito
        GROUP BY PersonaId
    ),
    Clasificados AS
    (
        SELECT
            CP.PersonaId,
            CP.CreditosMora,
            CAST(CP.SaldoMora AS DECIMAL(18, 2)) AS SaldoMora,
            CP.PrimeraCuotaVencida,
            CP.FechaUltimoPago,
            CASE
                WHEN CP.PrimeraCuotaVencida IS NULL THEN 0
                ELSE
                    DATEDIFF(DAY, CP.PrimeraCuotaVencida, @Hoy)
                    - DATEDIFF
                      (
                          WEEK,
                          DATEADD(DAY, -1, CP.PrimeraCuotaVencida),
                          @Hoy
                      )
            END AS DiasAtraso,
            CAST
            (
                CASE
                    WHEN CP.PagadoEnCreditosMora <= 0 THEN 'NUNCA_PAGO'
                    WHEN CP.PagoDesdeInicioMora = 0 THEN 'DEJO_PAGAR'
                    ELSE 'PAGA_CON_ATRASO'
                END
                AS VARCHAR(30)
            ) AS CodigoClasificacion,
            CASE
                WHEN CP.OficinaIdMin = CP.OficinaIdMax THEN CP.OficinaIdMin
                ELSE NULL
            END AS OficinaId,
            CASE
                WHEN CP.GestorIdMin = CP.GestorIdMax THEN CP.GestorIdMin
                ELSE NULL
            END AS GestorId
        FROM CarteraPersona CP
        WHERE CP.CreditosMora > 0
          AND CP.SaldoMora > 0
    )
    SELECT
        CAST(C.PersonaId AS INT) AS PersonaId,
        CAST(P.NombreCompleto AS VARCHAR(250)) AS NombreCompleto,
        CAST(P.NumeroDocumento AS VARCHAR(12)) AS NumeroDocumento,
        CAST(P.Celular1 AS VARCHAR(10)) AS Celular,
        CAST(C.OficinaId AS INT) AS OficinaId,
        CAST
        (
            CASE
                WHEN C.OficinaId IS NULL THEN 'VARIAS OFICINAS'
                ELSE ISNULL(O.Denominacion, 'OFICINA SIN NOMBRE')
            END
            AS VARCHAR(100)
        ) AS Oficina,
        CAST(C.GestorId AS INT) AS GestorId,
        CAST
        (
            CASE
                WHEN C.GestorId IS NULL THEN 'VARIOS'
                ELSE ISNULL(U.NombreUsuario, 'SIN USUARIO')
            END
            AS NVARCHAR(50)
        ) AS GestorUsuario,
        CAST
        (
            CASE
                WHEN C.GestorId IS NULL THEN 'VARIOS GESTORES'
                ELSE ISNULL(PG.NombreCompleto, U.NombreUsuario)
            END
            AS NVARCHAR(250)
        ) AS GestorNombre,
        CAST(C.CreditosMora AS INT) AS CreditosMora,
        CAST(C.SaldoMora AS DECIMAL(18, 2)) AS SaldoMora,
        CAST(C.PrimeraCuotaVencida AS DATETIME) AS PrimeraCuotaVencida,
        CAST(C.FechaUltimoPago AS DATETIME) AS FechaUltimoPago,
        CAST(C.DiasAtraso AS INT) AS DiasAtraso,
        CAST(C.CodigoClasificacion AS VARCHAR(30)) AS CodigoClasificacion,
        CAST
        (
            CASE C.CodigoClasificacion
                WHEN 'NUNCA_PAGO' THEN 'Nunca pagó'
                WHEN 'DEJO_PAGAR' THEN 'Dejó de pagar'
                ELSE 'Paga con atraso'
            END
            AS VARCHAR(30)
        ) AS Clasificacion,
        CAST(@Hoy AS DATETIME) AS FechaCorte
    FROM Clasificados C
    INNER JOIN MAESTRO.Persona P
        ON P.PersonaId = C.PersonaId
    LEFT JOIN MAESTRO.Oficina O
        ON O.OficinaId = C.OficinaId
    LEFT JOIN MAESTRO.Usuario U
        ON U.UsuarioId = C.GestorId
    LEFT JOIN MAESTRO.Persona PG
        ON PG.PersonaId = U.PersonaId
    WHERE @Tipo = 'TODOS'
       OR
       (
           @Tipo = 'SIN_PAGO'
           AND C.CodigoClasificacion IN ('NUNCA_PAGO', 'DEJO_PAGAR')
       )
       OR C.CodigoClasificacion = @Tipo
    ORDER BY
        CASE C.CodigoClasificacion
            WHEN 'NUNCA_PAGO' THEN 1
            WHEN 'DEJO_PAGAR' THEN 2
            ELSE 3
        END,
        C.DiasAtraso DESC,
        C.SaldoMora DESC,
        P.NombreCompleto;
END
GO
GO
PRINT N'OK seccion 2026-10-07-usp-morosidad-empresa.sql';
GO

GO
/* ===== INICIO 2026-10-06-prendario-drop-fecha-notif-whatsapp.sql ===== */
GO
-- Retira columna de idempotencia WhatsApp en CREDITO.Credito (no se usa en operación).
-- El aviso manual (wa.me) y Cloud API, si se activan, no persisten marca en BD.
-- Idempotente.

IF COL_LENGTH(N'CREDITO.Credito', N'FechaNotifWhatsapp3d') IS NOT NULL
BEGIN
    ALTER TABLE CREDITO.Credito DROP COLUMN FechaNotifWhatsapp3d;
END
GO
GO
PRINT N'OK seccion 2026-10-06-prendario-drop-fecha-notif-whatsapp.sql';
GO

GO
/* ===== INICIO 2026-10-10-usp-solicitar-condonacion-nullsafe.sql ===== */
GO
-- Fix: usp_SolicitarCondonacion fallaba con NULL en TotalPago cuando
-- no hay pagos CUO (SUM → NULL) o Interes/MontoCredito nulos.
-- Sintoma FE: "No se pudo registrar la solicitud de condonación."

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC [CREDITO].[usp_SolicitarCondonacion]
    @CajaDiarioId INT,
    @CreditoId INT,
    @MoraCondonacion DECIMAL(15, 2)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MontoCredito DECIMAL(15, 2) = (
        SELECT
            ISNULL(MontoCredito, 0)
            + ISNULL(MontoCredito, 0) * ISNULL(Interes, 0) / 100.0
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
    );

    IF @MontoCredito IS NULL
    BEGIN
        RAISERROR(N'No existe el crédito indicado para condonación.', 16, 1);
        RETURN;
    END;

    DECLARE @Pagos DECIMAL(15, 2) = ISNULL((
        SELECT SUM(ImportePago)
        FROM CREDITO.MovimientoCaja
        WHERE CreditoId = @CreditoId
          AND ImportePago > 0
          AND Operacion = N'CUO'
    ), 0);

    INSERT CREDITO.CreditoCondonacion
    (
        CreditoId,
        CajaDiarioId,
        Fecha,
        MoraCondonacion,
        IndAprobado,
        TotalPago
    )
    VALUES
    (
        @CreditoId,
        @CajaDiarioId,
        dbo.ufnFecha(),
        @MoraCondonacion,
        CAST(0 AS bit),
        @MontoCredito - @Pagos + ISNULL(@MoraCondonacion, 0)
    );
END
GO
GO
PRINT N'OK seccion 2026-10-10-usp-solicitar-condonacion-nullsafe.sql';
GO

GO
/* ===== INICIO 2026-10-16-usp-cerrar-cajas-diarios-oficina.sql ===== */
GO
-- Multisucursal: usp_CerrarCajasDiarios
--
-- Producción (2026-10-16) tenía:
--   1) Destino FORZADO a bóveda principal de OficinaId = 1 (central / dashboard).
--   2) CTE MOVS_BASE: SaldoInicial como efectivo (TipoPagoId=1) UNION ALL movimientos
--      del día, consolidado por caja/tipo — correcto para no perder efectivo físico.
--   3) Origen de cajas ya filtrado por @OficinaId.
--
-- Cambio para autonomía de sucursales (Pichari, Huanta, Andahuaylas):
--   El destino pasa a ser la bóveda ABIERTA de @OficinaId (principal; si hay temporal
--   abierta de esa oficina, se usa la temporal — mismo criterio legacy del schema).
--   Se conserva la reestructuración de efectivo (MOVS_BASE).
--
-- Ver: docs/migration/PLAN-MULTISUCURSAL-PRENDARIO.md (B-01)

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC [CREDITO].[usp_CerrarCajasDiarios]
    @UsuarioCierreId INT,
    @OficinaId INT = 1,
    @Sobrante DECIMAL(15, 2) = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @FechaTexto VARCHAR(20) = 'CIERRE ' + CONVERT(varchar, @Fecha, 103) + ' ';

    -- Destino: bóveda de LA MISMA oficina que cierra (no forzar central / OficinaId=1)
    DECLARE @BovedaId INT = (
        SELECT BovedaId
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = 0
          AND IndTemporal = 0
    );

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = 0
          AND IndTemporal = 1
    )
        SET @BovedaId = (
            SELECT BovedaId
            FROM CREDITO.Boveda
            WHERE OficinaId = @OficinaId
              AND IndCierre = 0
              AND IndTemporal = 1
        );

    IF @BovedaId IS NULL
    BEGIN
        RAISERROR(
            N'No se pueden cerrar las cajas: no existe una bóveda activa abierta en la oficina.',
            16,
            1
        );
        RETURN;
    END;

    -- Saldo inicial físico (efectivo) + movimientos del día (bancos / apps), por oficina
    ;WITH MOVS_BASE AS (
        SELECT
            C.Denominacion AS Caja,
            CD.CajaDiarioId,
            1 AS TipoPagoId,
            CD.SaldoInicial AS Importe
        FROM CREDITO.CajaDiario CD
        INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
        WHERE CD.IndCierre = 1
          AND CD.TransBoveda = 0
          AND C.OficinaId = @OficinaId

        UNION ALL

        SELECT
            C.Denominacion AS Caja,
            CD.CajaDiarioId,
            MC.TipoPagoId,
            SUM(IIF(MC.IndEntrada = 1, MC.ImportePago, -MC.ImportePago)) AS Importe
        FROM CREDITO.MovimientoCaja MC
        INNER JOIN CREDITO.CajaDiario CD ON CD.CajaDiarioId = MC.CajaDiarioId
        INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
        WHERE CD.IndCierre = 1
          AND CD.TransBoveda = 0
          AND C.OficinaId = @OficinaId
          AND MC.Estado = 1
        GROUP BY C.Denominacion, CD.CajaDiarioId, MC.TipoPagoId
    ),
    CUENTAS AS (
        SELECT
            Caja,
            CajaDiarioId,
            TipoPagoId,
            SUM(Importe) AS Importe
        FROM MOVS_BASE
        GROUP BY Caja, CajaDiarioId, TipoPagoId
    )
    INSERT INTO CREDITO.BovedaMov (
        BovedaId,
        CodOperacion,
        Glosa,
        CajaDiarioId,
        TipoPagoId,
        Importe,
        IndEntrada,
        Estado,
        UsuarioRegId,
        FechaReg
    )
    SELECT
        @BovedaId,
        'TRE',
        @FechaTexto + CU.Caja,
        CU.CajaDiarioId,
        CU.TipoPagoId,
        ROUND(CU.Importe, 1),
        1,
        1,
        @UsuarioCierreId,
        @Fecha
    FROM CUENTAS CU
    ORDER BY CU.CajaDiarioId, CU.TipoPagoId;

    IF @Sobrante > 0
        INSERT INTO CREDITO.BovedaMov (
            BovedaId,
            CodOperacion,
            Glosa,
            CajaDiarioId,
            TipoPagoId,
            Importe,
            IndEntrada,
            Estado,
            UsuarioRegId,
            FechaReg
        )
        VALUES (
            @BovedaId,
            'TRE',
            @FechaTexto + 'SOBRANTE',
            0,
            1,
            @Sobrante,
            1,
            1,
            @UsuarioCierreId,
            @Fecha
        );

    UPDATE CD
    SET CD.TransBoveda = 1
    FROM CREDITO.CajaDiario CD
    INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
    WHERE CD.IndCierre = 1
      AND CD.TransBoveda = 0
      AND C.OficinaId = @OficinaId;

    EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId;
END
GO
GO
PRINT N'OK seccion 2026-10-16-usp-cerrar-cajas-diarios-oficina.sql';
GO

GO
/* ===== INICIO 2026-10-20-usp-devolver-prenda.sql ===== */
GO
-- T-C2: transición CREDITO.Prenda.Estado EN CUSTODIA → DEVUELTO.
-- No crea tablas ni columnas. Fecha/usuario = FechaMod + UsuarioModId (ya existen).
-- No se libera garantía de un crédito DES (el cliente aún debe).
-- Sede = OficinaId del crédito (JWT).

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC [CREDITO].[usp_DevolverPrenda]
    @PrendaId BIGINT,
    @UsuarioId INT,
    @OficinaId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @PrendaId IS NULL OR @PrendaId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
    BEGIN
        RAISERROR(N'Datos de devolución incompletos.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.Prenda AS p
        INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
        WHERE p.PrendaId = @PrendaId
          AND c.OficinaId = @OficinaId
          AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
    )
    BEGIN
        RAISERROR(N'Bien no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Prenda
        WHERE PrendaId = @PrendaId
          AND Estado = N'DEVUELTO'
    )
    BEGIN
        RAISERROR(N'El bien ya fue devuelto.', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Prenda
        WHERE PrendaId = @PrendaId
          AND Estado <> N'EN CUSTODIA'
    )
    BEGIN
        RAISERROR(N'Solo se devuelven bienes en custodia.', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Prenda AS p
        INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
        WHERE p.PrendaId = @PrendaId
          AND c.Estado = N'DES'
    )
    BEGIN
        RAISERROR(N'No se puede devolver mientras el crédito está desembolsado.', 16, 1);
        RETURN;
    END;

    UPDATE CREDITO.Prenda
    SET Estado = N'DEVUELTO',
        UsuarioModId = @UsuarioId,
        FechaMod = dbo.ufnFecha()
    WHERE PrendaId = @PrendaId
      AND Estado = N'EN CUSTODIA';

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'No se actualizó el bien.', 16, 1);
        RETURN;
    END;

    SELECT
        p.PrendaId,
        p.CreditoId,
        p.Estado,
        p.FechaMod
    FROM CREDITO.Prenda AS p
    WHERE p.PrendaId = @PrendaId;
END
GO
GO
PRINT N'OK seccion 2026-10-20-usp-devolver-prenda.sql';
GO

GO
/* ===== INICIO 2026-10-21-pla-uif.sql ===== */
GO
-- T-C7: PLA/UIF con piso legal (D.S. 020-2017-JUS art. 24.4 / 25).
-- No inventa umbral en soles. Pisos en USD. TC = promedio venta SBS mes anterior.
-- Gerencia solo puede bajar el piso (24.4.c). ROS no tiene monto.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'CREDITO.PlaParametro', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.PlaParametro (
        PlaParametroId      int NOT NULL CONSTRAINT PK_PlaParametro PRIMARY KEY,
        IndividualUsd       decimal(18, 2) NOT NULL,
        MultipleUsd         decimal(18, 2) NOT NULL,
        UmbralIndividualMenorUsd decimal(18, 2) NULL,
        UmbralMultipleMenorUsd   decimal(18, 2) NULL,
        TcVentaMesAnterior  decimal(12, 4) NULL,
        TcPeriodo           char(7) NULL,
        FuenteNorma         varchar(80) NOT NULL,
        UsuarioModId        int NULL,
        FechaMod            datetime NULL,
        CONSTRAINT CK_PlaParametro_UnaFila CHECK (PlaParametroId = 1)
    );

    INSERT INTO CREDITO.PlaParametro (
        PlaParametroId, IndividualUsd, MultipleUsd, FuenteNorma)
    VALUES (1, 10000.00, 50000.00, N'D.S. 020-2017-JUS art. 24.4');
END
GO

IF OBJECT_ID(N'CREDITO.PlaInusual', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.PlaInusual (
        PlaInusualId    int IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_PlaInusual PRIMARY KEY,
        OficinaId       int NOT NULL,
        PersonaId       int NOT NULL,
        CreditoId       int NULL,
        Calificacion    varchar(12) NOT NULL,
        Motivo          varchar(500) NOT NULL,
        UsuarioRegId    int NOT NULL,
        FechaReg        datetime NOT NULL,
        CONSTRAINT CK_PlaInusual_Calificacion
            CHECK (Calificacion IN (N'INUSUAL', N'SOSPECHOSA', N'DESCARTADA'))
    );

    CREATE INDEX IX_PlaInusual_Oficina_Fecha
        ON CREDITO.PlaInusual (OficinaId, FechaReg DESC);
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_PlaObtenerParametro]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.PlaParametroId,
        p.IndividualUsd,
        p.MultipleUsd,
        p.UmbralIndividualMenorUsd,
        p.UmbralMultipleMenorUsd,
        CAST(COALESCE(p.UmbralIndividualMenorUsd, p.IndividualUsd) AS decimal(18, 2))
            AS IndividualEfectivoUsd,
        CAST(COALESCE(p.UmbralMultipleMenorUsd, p.MultipleUsd) AS decimal(18, 2))
            AS MultipleEfectivoUsd,
        p.TcVentaMesAnterior,
        p.TcPeriodo,
        p.FuenteNorma,
        CASE WHEN p.TcVentaMesAnterior IS NULL OR p.TcVentaMesAnterior <= 0
             THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS TieneTipoCambio
    FROM CREDITO.PlaParametro AS p
    WHERE p.PlaParametroId = 1;
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_PlaGuardarParametro]
    @TcVentaMesAnterior decimal(12, 4) = NULL,
    @TcPeriodo char(7) = NULL,
    @UmbralIndividualMenorUsd decimal(18, 2) = NULL,
    @UmbralMultipleMenorUsd decimal(18, 2) = NULL,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Usuario inválido.', 16, 1);
        RETURN;
    END;

    DECLARE @IndividualUsd decimal(18, 2);
    DECLARE @MultipleUsd decimal(18, 2);

    SELECT
        @IndividualUsd = IndividualUsd,
        @MultipleUsd = MultipleUsd
    FROM CREDITO.PlaParametro
    WHERE PlaParametroId = 1;

    IF @IndividualUsd IS NULL
    BEGIN
        RAISERROR(N'Falta fila CREDITO.PlaParametro.', 16, 1);
        RETURN;
    END;

    IF @TcVentaMesAnterior IS NOT NULL AND @TcVentaMesAnterior <= 0
    BEGIN
        RAISERROR(N'El tipo de cambio SBS debe ser mayor que cero.', 16, 1);
        RETURN;
    END;

    IF @UmbralIndividualMenorUsd IS NOT NULL
       AND (@UmbralIndividualMenorUsd <= 0 OR @UmbralIndividualMenorUsd > @IndividualUsd)
    BEGIN
        RAISERROR(N'El umbral menor individual debe ser > 0 y no superar el piso legal.', 16, 1);
        RETURN;
    END;

    IF @UmbralMultipleMenorUsd IS NOT NULL
       AND (@UmbralMultipleMenorUsd <= 0 OR @UmbralMultipleMenorUsd > @MultipleUsd)
    BEGIN
        RAISERROR(N'El umbral menor múltiple debe ser > 0 y no superar el piso legal.', 16, 1);
        RETURN;
    END;

    UPDATE CREDITO.PlaParametro
    SET TcVentaMesAnterior = @TcVentaMesAnterior,
        TcPeriodo = NULLIF(LTRIM(RTRIM(@TcPeriodo)), N''),
        UmbralIndividualMenorUsd = @UmbralIndividualMenorUsd,
        UmbralMultipleMenorUsd = @UmbralMultipleMenorUsd,
        UsuarioModId = @UsuarioId,
        FechaMod = dbo.ufnFecha()
    WHERE PlaParametroId = 1;

    EXEC CREDITO.usp_PlaObtenerParametro;
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_PlaCandidatosRo]
    @OficinaId int,
    @Anio int,
    @Mes int
AS
BEGIN
    SET NOCOUNT ON;

    IF @OficinaId IS NULL OR @OficinaId < 1
       OR @Anio IS NULL OR @Anio < 2000
       OR @Mes IS NULL OR @Mes < 1 OR @Mes > 12
    BEGIN
        RAISERROR(N'Oficina y periodo inválidos.', 16, 1);
        RETURN;
    END;

    DECLARE @Desde datetime = DATEFROMPARTS(@Anio, @Mes, 1);
    DECLARE @Hasta datetime = DATEADD(MONTH, 1, @Desde);
    DECLARE @IndividualUsd decimal(18, 2);
    DECLARE @MultipleUsd decimal(18, 2);
    DECLARE @Tc decimal(12, 4);
    DECLARE @TieneTc bit;
    DECLARE @UmbralIndividualPen decimal(18, 2);
    DECLARE @UmbralMultiplePen decimal(18, 2);

    SELECT
        @IndividualUsd = COALESCE(UmbralIndividualMenorUsd, IndividualUsd),
        @MultipleUsd = COALESCE(UmbralMultipleMenorUsd, MultipleUsd),
        @Tc = TcVentaMesAnterior
    FROM CREDITO.PlaParametro
    WHERE PlaParametroId = 1;

    SET @TieneTc = CASE WHEN @Tc IS NULL OR @Tc <= 0 THEN 0 ELSE 1 END;
    SET @UmbralIndividualPen = CASE WHEN @TieneTc = 1 THEN @IndividualUsd * @Tc END;
    SET @UmbralMultiplePen = CASE WHEN @TieneTc = 1 THEN @MultipleUsd * @Tc END;

    ;WITH Ops AS (
        SELECT
            CAST(N'DESEMBOLSO' AS varchar(16)) AS Tipo,
            c.CreditoId,
            c.PersonaId,
            c.OficinaId,
            CAST(COALESCE(c.MontoDesembolso, c.MontoCredito) AS decimal(16, 2)) AS Monto,
            CAST(COALESCE(c.FechaDesembolso, c.FechaReg) AS datetime) AS FechaOp
        FROM CREDITO.Credito AS c
        WHERE c.OficinaId = @OficinaId
          AND c.FechaDesembolso IS NOT NULL
          AND c.FechaDesembolso >= @Desde
          AND c.FechaDesembolso < @Hasta

        UNION ALL

        SELECT
            CAST(N'COBRANZA' AS varchar(16)),
            mc.CreditoId,
            COALESCE(mc.PersonaId, c.PersonaId),
            cj.OficinaId,
            CAST(mc.ImportePago AS decimal(16, 2)),
            mc.FechaReg
        FROM CREDITO.MovimientoCaja AS mc
        INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = mc.CajaDiarioId
        INNER JOIN CREDITO.Caja AS cj ON cj.CajaId = cd.CajaId
        LEFT JOIN CREDITO.Credito AS c ON c.CreditoId = mc.CreditoId
        WHERE cj.OficinaId = @OficinaId
          AND mc.Estado = CAST(1 AS bit)
          AND mc.IndEntrada = CAST(1 AS bit)
          AND mc.Operacion = 'CUO'
          AND mc.ImportePago > 0
          AND mc.FechaReg >= @Desde
          AND mc.FechaReg < @Hasta
    ),
    Multiples AS (
        SELECT
            o.PersonaId,
            SUM(o.Monto) AS MontoMes
        FROM Ops AS o
        WHERE o.PersonaId IS NOT NULL
        GROUP BY o.PersonaId
    )
    SELECT
        o.Tipo,
        o.CreditoId,
        o.PersonaId,
        p.NumeroDocumento,
        p.NombreCompleto,
        o.Monto,
        o.FechaOp,
        CASE
            WHEN @TieneTc = 0 THEN CAST(0 AS bit)
            WHEN o.Monto >= @UmbralIndividualPen THEN CAST(1 AS bit)
            WHEN m.MontoMes >= @UmbralMultiplePen THEN CAST(1 AS bit)
            ELSE CAST(0 AS bit)
        END AS SuperaUmbral,
        CASE
            WHEN @TieneTc = 0 THEN N'Falta tipo de cambio SBS (promedio venta mes anterior)'
            WHEN o.Monto >= @UmbralIndividualPen THEN N'RO individual'
            WHEN m.MontoMes >= @UmbralMultiplePen THEN N'RO múltiples del mes'
            ELSE N'Bajo umbral'
        END AS MotivoRo,
        CASE WHEN NULLIF(LTRIM(RTRIM(p.NumeroDocumento)), N'') IS NULL
             THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS FaltaDocumento,
        CASE WHEN NULLIF(LTRIM(RTRIM(p.Direccion)), N'') IS NULL
             THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS FaltaDireccion,
        CASE WHEN cl.ActividadEconId IS NULL
             THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS FaltaOcupacion,
        @IndividualUsd AS IndividualEfectivoUsd,
        @MultipleUsd AS MultipleEfectivoUsd,
        @Tc AS TcVentaMesAnterior,
        @UmbralIndividualPen AS UmbralIndividualPen,
        @UmbralMultiplePen AS UmbralMultiplePen,
        @TieneTc AS TieneTipoCambio,
        m.MontoMes
    FROM Ops AS o
    LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = o.PersonaId
    LEFT JOIN MAESTRO.Cliente AS cl ON cl.PersonaId = o.PersonaId
    LEFT JOIN Multiples AS m ON m.PersonaId = o.PersonaId
    ORDER BY
        CASE
            WHEN @TieneTc = 1 AND o.Monto >= @UmbralIndividualPen THEN 0
            WHEN @TieneTc = 1 AND m.MontoMes >= @UmbralMultiplePen THEN 1
            ELSE 2
        END,
        o.Monto DESC,
        o.FechaOp DESC;
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_PlaRegistrarInusual]
    @OficinaId int,
    @PersonaId int,
    @CreditoId int = NULL,
    @Calificacion varchar(12),
    @Motivo varchar(500),
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @OficinaId IS NULL OR @OficinaId < 1
       OR @PersonaId IS NULL OR @PersonaId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
       OR NULLIF(LTRIM(RTRIM(@Motivo)), N'') IS NULL
    BEGIN
        RAISERROR(N'Datos de bitácora incompletos.', 16, 1);
        RETURN;
    END;

    SET @Calificacion = UPPER(LTRIM(RTRIM(@Calificacion)));
    IF @Calificacion NOT IN (N'INUSUAL', N'SOSPECHOSA', N'DESCARTADA')
    BEGIN
        RAISERROR(N'Calificación no válida.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM MAESTRO.Persona WHERE PersonaId = @PersonaId)
    BEGIN
        RAISERROR(N'Persona no encontrada.', 16, 1);
        RETURN;
    END;

    IF @CreditoId IS NOT NULL AND @CreditoId > 0
       AND NOT EXISTS (
            SELECT 1
            FROM CREDITO.Credito
            WHERE CreditoId = @CreditoId
              AND OficinaId = @OficinaId
              AND PersonaId = @PersonaId)
    BEGIN
        RAISERROR(N'Crédito no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    INSERT INTO CREDITO.PlaInusual (
        OficinaId, PersonaId, CreditoId, Calificacion, Motivo, UsuarioRegId, FechaReg)
    VALUES (
        @OficinaId,
        @PersonaId,
        NULLIF(@CreditoId, 0),
        @Calificacion,
        LEFT(LTRIM(RTRIM(@Motivo)), 500),
        @UsuarioId,
        dbo.ufnFecha());

    SELECT
        SCOPE_IDENTITY() AS PlaInusualId,
        @Calificacion AS Calificacion;
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_PlaListarInusual]
    @OficinaId int,
    @Anio int,
    @Mes int
AS
BEGIN
    SET NOCOUNT ON;

    IF @OficinaId IS NULL OR @OficinaId < 1
       OR @Anio IS NULL OR @Anio < 2000
       OR @Mes IS NULL OR @Mes < 1 OR @Mes > 12
    BEGIN
        RAISERROR(N'Oficina y periodo inválidos.', 16, 1);
        RETURN;
    END;

    DECLARE @Desde datetime = DATEFROMPARTS(@Anio, @Mes, 1);
    DECLARE @Hasta datetime = DATEADD(MONTH, 1, @Desde);

    SELECT
        i.PlaInusualId,
        i.OficinaId,
        i.PersonaId,
        p.NumeroDocumento,
        p.NombreCompleto,
        i.CreditoId,
        i.Calificacion,
        i.Motivo,
        i.UsuarioRegId,
        u.NombreUsuario,
        i.FechaReg
    FROM CREDITO.PlaInusual AS i
    INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = i.PersonaId
    LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = i.UsuarioRegId
    WHERE i.OficinaId = @OficinaId
      AND i.FechaReg >= @Desde
      AND i.FechaReg < @Hasta
    ORDER BY i.FechaReg DESC;
END
GO
GO
PRINT N'OK seccion 2026-10-21-pla-uif.sql';
GO

GO
/* ===== INICIO 2026-10-21-comunicados-agenda.sql ===== */
GO
-- Comunicados (avisos/reglamentos) + agenda. Adjuntos en SQL (App Service sin disco durable).

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'MAESTRO.Comunicado', N'U') IS NULL
BEGIN
    CREATE TABLE MAESTRO.Comunicado (
        ComunicadoId    int IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_Comunicado PRIMARY KEY,
        Titulo          varchar(160) NOT NULL,
        Cuerpo          varchar(4000) NOT NULL,
        Alcance         varchar(12) NOT NULL,
        OficinaId       int NULL,
        FechaIni        datetime NOT NULL,
        FechaFin        datetime NULL,
        Estado          bit NOT NULL CONSTRAINT DF_Comunicado_Estado DEFAULT (1),
        UsuarioRegId    int NOT NULL,
        FechaReg        datetime NOT NULL,
        CONSTRAINT CK_Comunicado_Alcance CHECK (Alcance IN (N'RED', N'OFICINA'))
    );
    CREATE INDEX IX_Comunicado_Vigencia ON MAESTRO.Comunicado (Estado, FechaIni, FechaFin);
END
GO

IF OBJECT_ID(N'MAESTRO.ComunicadoAdjunto', N'U') IS NULL
BEGIN
    CREATE TABLE MAESTRO.ComunicadoAdjunto (
        AdjuntoId       int IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_ComunicadoAdjunto PRIMARY KEY,
        ComunicadoId    int NOT NULL,
        NombreOriginal  varchar(200) NOT NULL,
        ContentType     varchar(80) NOT NULL,
        TamanoBytes     int NOT NULL,
        Contenido       varbinary(max) NOT NULL,
        CONSTRAINT FK_ComunicadoAdjunto_Comunicado
            FOREIGN KEY (ComunicadoId) REFERENCES MAESTRO.Comunicado (ComunicadoId)
    );
END
GO

IF OBJECT_ID(N'MAESTRO.ComunicadoLectura', N'U') IS NULL
BEGIN
    CREATE TABLE MAESTRO.ComunicadoLectura (
        ComunicadoId    int NOT NULL,
        UsuarioId       int NOT NULL,
        FechaLectura    datetime NOT NULL,
        CONSTRAINT PK_ComunicadoLectura PRIMARY KEY (ComunicadoId, UsuarioId)
    );
END
GO

IF OBJECT_ID(N'MAESTRO.AgendaEvento', N'U') IS NULL
BEGIN
    CREATE TABLE MAESTRO.AgendaEvento (
        EventoId            int IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_AgendaEvento PRIMARY KEY,
        Titulo              varchar(160) NOT NULL,
        Notas               varchar(1000) NULL,
        FechaIni            datetime NOT NULL,
        FechaFin            datetime NULL,
        TodoElDia           bit NOT NULL CONSTRAINT DF_Agenda_TodoElDia DEFAULT (0),
        RecordatorioMinutos int NULL,
        Alcance             varchar(12) NOT NULL,
        OficinaId           int NULL,
        UsuarioId           int NOT NULL,
        Estado              bit NOT NULL CONSTRAINT DF_Agenda_Estado DEFAULT (1),
        UsuarioRegId        int NOT NULL,
        FechaReg            datetime NOT NULL,
        CONSTRAINT CK_Agenda_Alcance CHECK (Alcance IN (N'PERSONAL', N'OFICINA'))
    );
    CREATE INDEX IX_Agenda_Usuario ON MAESTRO.AgendaEvento (UsuarioId, FechaIni);
    CREATE INDEX IX_Agenda_Oficina ON MAESTRO.AgendaEvento (OficinaId, FechaIni);
END
GO

IF OBJECT_ID(N'MAESTRO.AgendaDescarte', N'U') IS NULL
BEGIN
    CREATE TABLE MAESTRO.AgendaDescarte (
        EventoId    int NOT NULL,
        UsuarioId   int NOT NULL,
        FechaReg    datetime NOT NULL,
        CONSTRAINT PK_AgendaDescarte PRIMARY KEY (EventoId, UsuarioId)
    );
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoListar]
    @OficinaId int,
    @UsuarioId int,
    @IncluirVencidos bit = 0,
    @Gestion bit = 0,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ahora datetime = dbo.ufnFecha();

    SELECT
        c.ComunicadoId,
        c.Titulo,
        c.Cuerpo,
        c.Alcance,
        c.OficinaId,
        o.Denominacion AS OficinaNombre,
        c.FechaIni,
        c.FechaFin,
        CASE WHEN c.FechaFin IS NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Permanente,
        CASE WHEN c.FechaIni <= @Ahora
              AND (c.FechaFin IS NULL OR c.FechaFin >= @Ahora)
             THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Vigente,
        CASE WHEN l.UsuarioId IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS Leido,
        (SELECT COUNT(*) FROM MAESTRO.ComunicadoAdjunto a WHERE a.ComunicadoId = c.ComunicadoId) AS Adjuntos,
        c.UsuarioRegId,
        u.NombreUsuario,
        c.FechaReg
    FROM MAESTRO.Comunicado AS c
    LEFT JOIN MAESTRO.ComunicadoLectura AS l
        ON l.ComunicadoId = c.ComunicadoId AND l.UsuarioId = @UsuarioId
    LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
    LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = c.OficinaId
    WHERE c.Estado = CAST(1 AS bit)
      AND (
            (@Gestion = 1 AND @EsAdmin = 1)
            OR c.Alcance = N'RED'
            OR c.OficinaId = @OficinaId
          )
      AND (
            @IncluirVencidos = 1
            OR (c.FechaIni <= @Ahora AND (c.FechaFin IS NULL OR c.FechaFin >= @Ahora))
          )
    ORDER BY
        CASE WHEN l.UsuarioId IS NULL THEN 0 ELSE 1 END,
        CASE WHEN c.FechaFin IS NULL THEN 0 ELSE 1 END,
        c.FechaReg DESC;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoObtener]
    @ComunicadoId int,
    @OficinaId int,
    @UsuarioId int,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ahora datetime = dbo.ufnFecha();

    SELECT
        c.ComunicadoId,
        c.Titulo,
        c.Cuerpo,
        c.Alcance,
        c.OficinaId,
        o.Denominacion AS OficinaNombre,
        c.FechaIni,
        c.FechaFin,
        CASE WHEN c.FechaFin IS NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Permanente,
        CASE WHEN c.FechaIni <= @Ahora
              AND (c.FechaFin IS NULL OR c.FechaFin >= @Ahora)
             THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Vigente,
        CASE WHEN l.UsuarioId IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS Leido,
        (SELECT COUNT(*) FROM MAESTRO.ComunicadoAdjunto a WHERE a.ComunicadoId = c.ComunicadoId) AS Adjuntos,
        c.UsuarioRegId,
        u.NombreUsuario,
        c.FechaReg
    FROM MAESTRO.Comunicado AS c
    LEFT JOIN MAESTRO.ComunicadoLectura AS l
        ON l.ComunicadoId = c.ComunicadoId AND l.UsuarioId = @UsuarioId
    LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
    LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = c.OficinaId
    WHERE c.ComunicadoId = @ComunicadoId
      AND c.Estado = CAST(1 AS bit)
      AND (
            @EsAdmin = 1
            OR c.Alcance = N'RED'
            OR c.OficinaId = @OficinaId
          );

    SELECT
        a.AdjuntoId,
        a.ComunicadoId,
        a.NombreOriginal,
        a.ContentType,
        a.TamanoBytes
    FROM MAESTRO.ComunicadoAdjunto AS a
    INNER JOIN MAESTRO.Comunicado AS c ON c.ComunicadoId = a.ComunicadoId
    WHERE a.ComunicadoId = @ComunicadoId
      AND c.Estado = CAST(1 AS bit)
      AND (
            @EsAdmin = 1
            OR c.Alcance = N'RED'
            OR c.OficinaId = @OficinaId
          );
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoGuardar]
    @ComunicadoId int = NULL,
    @Titulo varchar(160),
    @Cuerpo varchar(4000),
    @Alcance varchar(12),
    @OficinaId int,
    @FechaIni datetime,
    @FechaFin datetime = NULL,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;

    IF NULLIF(LTRIM(RTRIM(@Titulo)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Cuerpo)), N'') IS NULL
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos del aviso incompletos.', 16, 1);
        RETURN;
    END;

    SET @Alcance = UPPER(LTRIM(RTRIM(@Alcance)));
    IF @Alcance NOT IN (N'RED', N'OFICINA')
    BEGIN
        RAISERROR(N'Alcance no válido.', 16, 1);
        RETURN;
    END;

    IF @Alcance = N'OFICINA' AND (@OficinaId IS NULL OR @OficinaId < 1)
    BEGIN
        RAISERROR(N'Elija la sede donde será visible el aviso.', 16, 1);
        RETURN;
    END;

    IF @Alcance = N'OFICINA'
       AND NOT EXISTS (SELECT 1 FROM MAESTRO.Oficina WHERE OficinaId = @OficinaId AND Estado = 1)
    BEGIN
        RAISERROR(N'La sede no existe o está inactiva.', 16, 1);
        RETURN;
    END;

    IF @FechaFin IS NOT NULL AND @FechaFin < @FechaIni
    BEGIN
        RAISERROR(N'La fecha fin no puede ser anterior al inicio.', 16, 1);
        RETURN;
    END;

    IF @ComunicadoId IS NULL OR @ComunicadoId < 1
    BEGIN
        INSERT INTO MAESTRO.Comunicado (
            Titulo, Cuerpo, Alcance, OficinaId, FechaIni, FechaFin, Estado, UsuarioRegId, FechaReg)
        VALUES (
            LEFT(LTRIM(RTRIM(@Titulo)), 160),
            LEFT(LTRIM(RTRIM(@Cuerpo)), 4000),
            @Alcance,
            CASE WHEN @Alcance = N'RED' THEN NULL ELSE @OficinaId END,
            @FechaIni,
            @FechaFin,
            1,
            @UsuarioId,
            dbo.ufnFecha());
        SET @ComunicadoId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE MAESTRO.Comunicado
        SET Titulo = LEFT(LTRIM(RTRIM(@Titulo)), 160),
            Cuerpo = LEFT(LTRIM(RTRIM(@Cuerpo)), 4000),
            Alcance = @Alcance,
            OficinaId = CASE WHEN @Alcance = N'RED' THEN NULL ELSE @OficinaId END,
            FechaIni = @FechaIni,
            FechaFin = @FechaFin
        WHERE ComunicadoId = @ComunicadoId
          AND Estado = CAST(1 AS bit);
        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR(N'Aviso no encontrado.', 16, 1);
            RETURN;
        END
    END;

    SELECT @ComunicadoId AS ComunicadoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoBaja]
    @ComunicadoId int,
    @UsuarioId int,
    @OficinaId int,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MAESTRO.Comunicado
    SET Estado = 0
    WHERE ComunicadoId = @ComunicadoId
      AND Estado = 1
      AND (
            @EsAdmin = 1
            OR (Alcance = N'OFICINA' AND OficinaId = @OficinaId)
          );
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'No puede retirar este aviso.', 16, 1);
        RETURN;
    END;
    SELECT @ComunicadoId AS ComunicadoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoMarcarLeido]
    @ComunicadoId int,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM MAESTRO.Comunicado
        WHERE ComunicadoId = @ComunicadoId AND Estado = 1)
    BEGIN
        RAISERROR(N'Aviso no encontrado.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM MAESTRO.ComunicadoLectura
        WHERE ComunicadoId = @ComunicadoId AND UsuarioId = @UsuarioId)
    BEGIN
        INSERT INTO MAESTRO.ComunicadoLectura (ComunicadoId, UsuarioId, FechaLectura)
        VALUES (@ComunicadoId, @UsuarioId, dbo.ufnFecha());
    END;
    SELECT @ComunicadoId AS ComunicadoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoAdjuntoIns]
    @ComunicadoId int,
    @NombreOriginal varchar(200),
    @ContentType varchar(80),
    @Contenido varbinary(max)
AS
BEGIN
    SET NOCOUNT ON;
    IF @Contenido IS NULL OR DATALENGTH(@Contenido) < 1
    BEGIN
        RAISERROR(N'Archivo vacío.', 16, 1);
        RETURN;
    END;
    INSERT INTO MAESTRO.ComunicadoAdjunto (
        ComunicadoId, NombreOriginal, ContentType, TamanoBytes, Contenido)
    VALUES (
        @ComunicadoId,
        LEFT(@NombreOriginal, 200),
        LEFT(@ContentType, 80),
        DATALENGTH(@Contenido),
        @Contenido);
    SELECT CAST(SCOPE_IDENTITY() AS int) AS AdjuntoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoAdjuntoGet]
    @AdjuntoId int,
    @OficinaId int,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.AdjuntoId,
        a.NombreOriginal,
        a.ContentType,
        a.TamanoBytes,
        a.Contenido
    FROM MAESTRO.ComunicadoAdjunto AS a
    INNER JOIN MAESTRO.Comunicado AS c ON c.ComunicadoId = a.ComunicadoId
    WHERE a.AdjuntoId = @AdjuntoId
      AND c.Estado = 1
      AND (
            @EsAdmin = 1
            OR c.Alcance = N'RED'
            OR c.OficinaId = @OficinaId
          );
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoAdjuntoDel]
    @AdjuntoId int,
    @OficinaId int,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    DELETE a
    FROM MAESTRO.ComunicadoAdjunto AS a
    INNER JOIN MAESTRO.Comunicado AS c ON c.ComunicadoId = a.ComunicadoId
    WHERE a.AdjuntoId = @AdjuntoId
      AND c.Estado = 1
      AND (
            @EsAdmin = 1
            OR (c.Alcance = N'OFICINA' AND c.OficinaId = @OficinaId)
          );
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'No puede quitar este archivo.', 16, 1);
        RETURN;
    END;
    SELECT @AdjuntoId AS AdjuntoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_AgendaListar]
    @OficinaId int,
    @UsuarioId int,
    @Desde datetime,
    @Hasta datetime
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.EventoId,
        e.Titulo,
        e.Notas,
        e.FechaIni,
        e.FechaFin,
        e.TodoElDia,
        e.RecordatorioMinutos,
        e.Alcance,
        e.OficinaId,
        e.UsuarioId,
        CASE WHEN e.UsuarioId = @UsuarioId THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS EsMio
    FROM MAESTRO.AgendaEvento AS e
    WHERE e.Estado = 1
      AND e.FechaIni < @Hasta
      AND COALESCE(e.FechaFin, e.FechaIni) >= @Desde
      AND (
            (e.Alcance = N'PERSONAL' AND e.UsuarioId = @UsuarioId)
            OR (e.Alcance = N'OFICINA' AND e.OficinaId = @OficinaId)
          )
    ORDER BY e.FechaIni;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_AgendaGuardar]
    @EventoId int = NULL,
    @Titulo varchar(160),
    @Notas varchar(1000) = NULL,
    @FechaIni datetime,
    @FechaFin datetime = NULL,
    @TodoElDia bit,
    @RecordatorioMinutos int = NULL,
    @Alcance varchar(12),
    @OficinaId int,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;
    IF NULLIF(LTRIM(RTRIM(@Titulo)), N'') IS NULL OR @FechaIni IS NULL
    BEGIN
        RAISERROR(N'Datos de agenda incompletos.', 16, 1);
        RETURN;
    END;
    SET @Alcance = UPPER(LTRIM(RTRIM(@Alcance)));
    IF @Alcance NOT IN (N'PERSONAL', N'OFICINA')
    BEGIN
        RAISERROR(N'Alcance de agenda no válido.', 16, 1);
        RETURN;
    END;

    IF @EventoId IS NULL OR @EventoId < 1
    BEGIN
        INSERT INTO MAESTRO.AgendaEvento (
            Titulo, Notas, FechaIni, FechaFin, TodoElDia, RecordatorioMinutos,
            Alcance, OficinaId, UsuarioId, Estado, UsuarioRegId, FechaReg)
        VALUES (
            LEFT(LTRIM(RTRIM(@Titulo)), 160),
            NULLIF(LTRIM(RTRIM(@Notas)), N''),
            @FechaIni,
            @FechaFin,
            @TodoElDia,
            @RecordatorioMinutos,
            @Alcance,
            CASE WHEN @Alcance = N'OFICINA' THEN @OficinaId ELSE NULL END,
            @UsuarioId,
            1,
            @UsuarioId,
            dbo.ufnFecha());
        SET @EventoId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE MAESTRO.AgendaEvento
        SET Titulo = LEFT(LTRIM(RTRIM(@Titulo)), 160),
            Notas = NULLIF(LTRIM(RTRIM(@Notas)), N''),
            FechaIni = @FechaIni,
            FechaFin = @FechaFin,
            TodoElDia = @TodoElDia,
            RecordatorioMinutos = @RecordatorioMinutos,
            Alcance = @Alcance,
            OficinaId = CASE WHEN @Alcance = N'OFICINA' THEN @OficinaId ELSE NULL END
        WHERE EventoId = @EventoId
          AND Estado = 1
          AND UsuarioId = @UsuarioId;
        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR(N'Evento no encontrado o no es suyo.', 16, 1);
            RETURN;
        END
    END;
    SELECT @EventoId AS EventoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_AgendaBaja]
    @EventoId int,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MAESTRO.AgendaEvento
    SET Estado = 0
    WHERE EventoId = @EventoId
      AND Estado = 1
      AND UsuarioId = @UsuarioId;
    SELECT @@ROWCOUNT AS Filas;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_AgendaDescartar]
    @EventoId int,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM MAESTRO.AgendaDescarte
        WHERE EventoId = @EventoId AND UsuarioId = @UsuarioId)
    BEGIN
        INSERT INTO MAESTRO.AgendaDescarte (EventoId, UsuarioId, FechaReg)
        VALUES (@EventoId, @UsuarioId, dbo.ufnFecha());
    END;
    SELECT @EventoId AS EventoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_BandejaNotificaciones]
    @OficinaId int,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ahora datetime = dbo.ufnFecha();

    SELECT
        CAST(N'COMUNICADO' AS varchar(16)) AS Tipo,
        c.ComunicadoId AS Id,
        c.Titulo,
        c.FechaReg AS Fecha,
        CAST(0 AS bit) AS PermanenteNo
    FROM MAESTRO.Comunicado AS c
    WHERE c.Estado = 1
      AND c.FechaIni <= @Ahora
      AND (c.FechaFin IS NULL OR c.FechaFin >= @Ahora)
      AND (c.Alcance = N'RED' OR c.OficinaId = @OficinaId)
      AND NOT EXISTS (
            SELECT 1 FROM MAESTRO.ComunicadoLectura l
            WHERE l.ComunicadoId = c.ComunicadoId AND l.UsuarioId = @UsuarioId)

    UNION ALL

    SELECT
        CAST(N'AGENDA' AS varchar(16)),
        e.EventoId,
        e.Titulo,
        e.FechaIni,
        CAST(0 AS bit)
    FROM MAESTRO.AgendaEvento AS e
    WHERE e.Estado = 1
      AND e.RecordatorioMinutos IS NOT NULL
      AND DATEADD(MINUTE, -e.RecordatorioMinutos, e.FechaIni) <= @Ahora
      AND e.FechaIni >= DATEADD(DAY, -1, @Ahora)
      AND (
            (e.Alcance = N'PERSONAL' AND e.UsuarioId = @UsuarioId)
            OR (e.Alcance = N'OFICINA' AND e.OficinaId = @OficinaId)
          )
      AND NOT EXISTS (
            SELECT 1 FROM MAESTRO.AgendaDescarte d
            WHERE d.EventoId = e.EventoId AND d.UsuarioId = @UsuarioId)
    ORDER BY Fecha DESC;
END
GO
GO
PRINT N'OK seccion 2026-10-21-comunicados-agenda.sql';
GO

GO
/* ===== INICIO 2026-10-21-comunicados-edicion.sql ===== */
GO
-- Completa avisos: destino una sede o toda la red, editar y eliminar.
-- Idempotente (CREATE OR ALTER). Aplicar también en Azure si ya corrió comunicados-agenda.sql.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoListar]
    @OficinaId int,
    @UsuarioId int,
    @IncluirVencidos bit = 0,
    @Gestion bit = 0,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ahora datetime = dbo.ufnFecha();

    SELECT
        c.ComunicadoId,
        c.Titulo,
        c.Cuerpo,
        c.Alcance,
        c.OficinaId,
        o.Denominacion AS OficinaNombre,
        c.FechaIni,
        c.FechaFin,
        CASE WHEN c.FechaFin IS NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Permanente,
        CASE WHEN c.FechaIni <= @Ahora
              AND (c.FechaFin IS NULL OR c.FechaFin >= @Ahora)
             THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Vigente,
        CASE WHEN l.UsuarioId IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS Leido,
        (SELECT COUNT(*) FROM MAESTRO.ComunicadoAdjunto a WHERE a.ComunicadoId = c.ComunicadoId) AS Adjuntos,
        c.UsuarioRegId,
        u.NombreUsuario,
        c.FechaReg
    FROM MAESTRO.Comunicado AS c
    LEFT JOIN MAESTRO.ComunicadoLectura AS l
        ON l.ComunicadoId = c.ComunicadoId AND l.UsuarioId = @UsuarioId
    LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
    LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = c.OficinaId
    WHERE c.Estado = CAST(1 AS bit)
      AND (
            (@Gestion = 1 AND @EsAdmin = 1)
            OR c.Alcance = N'RED'
            OR c.OficinaId = @OficinaId
          )
      AND (
            @IncluirVencidos = 1
            OR (c.FechaIni <= @Ahora AND (c.FechaFin IS NULL OR c.FechaFin >= @Ahora))
          )
    ORDER BY
        CASE WHEN l.UsuarioId IS NULL THEN 0 ELSE 1 END,
        CASE WHEN c.FechaFin IS NULL THEN 0 ELSE 1 END,
        c.FechaReg DESC;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoObtener]
    @ComunicadoId int,
    @OficinaId int,
    @UsuarioId int,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ahora datetime = dbo.ufnFecha();

    SELECT
        c.ComunicadoId,
        c.Titulo,
        c.Cuerpo,
        c.Alcance,
        c.OficinaId,
        o.Denominacion AS OficinaNombre,
        c.FechaIni,
        c.FechaFin,
        CASE WHEN c.FechaFin IS NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Permanente,
        CASE WHEN c.FechaIni <= @Ahora
              AND (c.FechaFin IS NULL OR c.FechaFin >= @Ahora)
             THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Vigente,
        CASE WHEN l.UsuarioId IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS Leido,
        (SELECT COUNT(*) FROM MAESTRO.ComunicadoAdjunto a WHERE a.ComunicadoId = c.ComunicadoId) AS Adjuntos,
        c.UsuarioRegId,
        u.NombreUsuario,
        c.FechaReg
    FROM MAESTRO.Comunicado AS c
    LEFT JOIN MAESTRO.ComunicadoLectura AS l
        ON l.ComunicadoId = c.ComunicadoId AND l.UsuarioId = @UsuarioId
    LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
    LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = c.OficinaId
    WHERE c.ComunicadoId = @ComunicadoId
      AND c.Estado = CAST(1 AS bit)
      AND (
            @EsAdmin = 1
            OR c.Alcance = N'RED'
            OR c.OficinaId = @OficinaId
          );

    SELECT
        a.AdjuntoId,
        a.ComunicadoId,
        a.NombreOriginal,
        a.ContentType,
        a.TamanoBytes
    FROM MAESTRO.ComunicadoAdjunto AS a
    INNER JOIN MAESTRO.Comunicado AS c ON c.ComunicadoId = a.ComunicadoId
    WHERE a.ComunicadoId = @ComunicadoId
      AND c.Estado = CAST(1 AS bit)
      AND (
            @EsAdmin = 1
            OR c.Alcance = N'RED'
            OR c.OficinaId = @OficinaId
          );
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoGuardar]
    @ComunicadoId int = NULL,
    @Titulo varchar(160),
    @Cuerpo varchar(4000),
    @Alcance varchar(12),
    @OficinaId int,
    @FechaIni datetime,
    @FechaFin datetime = NULL,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;

    IF NULLIF(LTRIM(RTRIM(@Titulo)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Cuerpo)), N'') IS NULL
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos del aviso incompletos.', 16, 1);
        RETURN;
    END;

    SET @Alcance = UPPER(LTRIM(RTRIM(@Alcance)));
    IF @Alcance NOT IN (N'RED', N'OFICINA')
    BEGIN
        RAISERROR(N'Alcance no válido.', 16, 1);
        RETURN;
    END;

    IF @Alcance = N'OFICINA' AND (@OficinaId IS NULL OR @OficinaId < 1)
    BEGIN
        RAISERROR(N'Elija la sede donde será visible el aviso.', 16, 1);
        RETURN;
    END;

    IF @Alcance = N'OFICINA'
       AND NOT EXISTS (SELECT 1 FROM MAESTRO.Oficina WHERE OficinaId = @OficinaId AND Estado = 1)
    BEGIN
        RAISERROR(N'La sede no existe o está inactiva.', 16, 1);
        RETURN;
    END;

    IF @FechaFin IS NOT NULL AND @FechaFin < @FechaIni
    BEGIN
        RAISERROR(N'La fecha fin no puede ser anterior al inicio.', 16, 1);
        RETURN;
    END;

    IF @ComunicadoId IS NULL OR @ComunicadoId < 1
    BEGIN
        INSERT INTO MAESTRO.Comunicado (
            Titulo, Cuerpo, Alcance, OficinaId, FechaIni, FechaFin, Estado, UsuarioRegId, FechaReg)
        VALUES (
            LEFT(LTRIM(RTRIM(@Titulo)), 160),
            LEFT(LTRIM(RTRIM(@Cuerpo)), 4000),
            @Alcance,
            CASE WHEN @Alcance = N'RED' THEN NULL ELSE @OficinaId END,
            @FechaIni,
            @FechaFin,
            1,
            @UsuarioId,
            dbo.ufnFecha());
        SET @ComunicadoId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE MAESTRO.Comunicado
        SET Titulo = LEFT(LTRIM(RTRIM(@Titulo)), 160),
            Cuerpo = LEFT(LTRIM(RTRIM(@Cuerpo)), 4000),
            Alcance = @Alcance,
            OficinaId = CASE WHEN @Alcance = N'RED' THEN NULL ELSE @OficinaId END,
            FechaIni = @FechaIni,
            FechaFin = @FechaFin
        WHERE ComunicadoId = @ComunicadoId
          AND Estado = CAST(1 AS bit);
        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR(N'Aviso no encontrado.', 16, 1);
            RETURN;
        END
    END;

    SELECT @ComunicadoId AS ComunicadoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoBaja]
    @ComunicadoId int,
    @UsuarioId int,
    @OficinaId int,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MAESTRO.Comunicado
    SET Estado = 0
    WHERE ComunicadoId = @ComunicadoId
      AND Estado = 1
      AND (
            @EsAdmin = 1
            OR (Alcance = N'OFICINA' AND OficinaId = @OficinaId)
          );
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'No puede retirar este aviso.', 16, 1);
        RETURN;
    END;
    SELECT @ComunicadoId AS ComunicadoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoAdjuntoGet]
    @AdjuntoId int,
    @OficinaId int,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.AdjuntoId,
        a.NombreOriginal,
        a.ContentType,
        a.TamanoBytes,
        a.Contenido
    FROM MAESTRO.ComunicadoAdjunto AS a
    INNER JOIN MAESTRO.Comunicado AS c ON c.ComunicadoId = a.ComunicadoId
    WHERE a.AdjuntoId = @AdjuntoId
      AND c.Estado = 1
      AND (
            @EsAdmin = 1
            OR c.Alcance = N'RED'
            OR c.OficinaId = @OficinaId
          );
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoAdjuntoDel]
    @AdjuntoId int,
    @OficinaId int,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    DELETE a
    FROM MAESTRO.ComunicadoAdjunto AS a
    INNER JOIN MAESTRO.Comunicado AS c ON c.ComunicadoId = a.ComunicadoId
    WHERE a.AdjuntoId = @AdjuntoId
      AND c.Estado = 1
      AND (
            @EsAdmin = 1
            OR (c.Alcance = N'OFICINA' AND c.OficinaId = @OficinaId)
          );
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'No puede quitar este archivo.', 16, 1);
        RETURN;
    END;
    SELECT @AdjuntoId AS AdjuntoId;
END
GO
GO
PRINT N'OK seccion 2026-10-21-comunicados-edicion.sql';
GO

GO
/* ===== INICIO 2026-10-23-comunicado-adjunto-identidad.sql ===== */
GO
-- SCOPE_IDENTITY() es numeric; CAST a int evita fallo de mapeo al devolver AdjuntoId.
-- Idempotente. No cambia el INSERT; solo el SELECT de retorno.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoAdjuntoIns]
    @ComunicadoId int,
    @NombreOriginal varchar(200),
    @ContentType varchar(80),
    @Contenido varbinary(max)
AS
BEGIN
    SET NOCOUNT ON;
    IF @Contenido IS NULL OR DATALENGTH(@Contenido) < 1
    BEGIN
        RAISERROR(N'Archivo vacío.', 16, 1);
        RETURN;
    END;
    INSERT INTO MAESTRO.ComunicadoAdjunto (
        ComunicadoId, NombreOriginal, ContentType, TamanoBytes, Contenido)
    VALUES (
        @ComunicadoId,
        LEFT(@NombreOriginal, 200),
        LEFT(@ContentType, 80),
        DATALENGTH(@Contenido),
        @Contenido);
    SELECT CAST(SCOPE_IDENTITY() AS int) AS AdjuntoId;
END
GO
GO
PRINT N'OK seccion 2026-10-23-comunicado-adjunto-identidad.sql';
GO

GO
/* ===== INICIO 2026-10-23-rechazar-solicitud-hijos.sql ===== */
GO
-- Rechazo de solicitud PEN. NO toca CREDITO.usp_Credito_Del (eso es ANULAR
-- del legacy: UPDATE Estado='ANU', no DELETE).
--
-- Legacy:
--   RechazarCredito → usp_OrdenVenta_Del (si hay OV) o usp_SolicitudCredito_Del
--   AnularCredito   → usp_Credito_Del
--
-- Este lote SOLO completa usp_SolicitudCredito_Del (hijos que bloquean
-- DELETE Credito: Aprobacion, Prenda, imagen, tarea, cargo, central).
-- NO reemplaza VENTAS.usp_OrdenVenta_Del: es USP de ventas del MVC; si
-- producción divergió, un CREATE OR ALTER entero rompe borrar OV.
-- La API moderna ya limpia esos hijos en C# antes de llamar a cualquiera
-- de los dos USP.
--
-- No se tocan CxC / caja / mora / condonación: esos créditos se anulan.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC CREDITO.[usp_SolicitudCredito_Del]
    @CreditoId INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreditoId IS NULL OR @CreditoId < 1
        RETURN;

    IF EXISTS (
        SELECT 1 FROM CREDITO.MovimientoCaja WHERE CreditoId = @CreditoId
        UNION ALL
        SELECT 1 FROM CREDITO.CuentaxCobrar WHERE CreditoId = @CreditoId
        UNION ALL
        SELECT 1 FROM CREDITO.CreditoMora WHERE CreditoId = @CreditoId
        UNION ALL
        SELECT 1 FROM CREDITO.CreditoCondonacion WHERE CreditoId = @CreditoId
    )
    BEGIN
        RAISERROR(N'No se puede rechazar: el crédito ya tiene cuentas por cobrar, pagos de caja o mora. Use anular.', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
          AND Estado <> 'PEN'
    )
    BEGIN
        RAISERROR(N'Solo se puede rechazar un crédito pendiente de aprobación (PEN). Si ya está aprobado o desembolsado, use anular.', 16, 1);
        RETURN;
    END

    IF OBJECT_ID(N'CREDITO.PlaInusual', N'U') IS NOT NULL
        UPDATE CREDITO.PlaInusual SET CreditoId = NULL WHERE CreditoId = @CreditoId;

    DELETE FROM CREDITO.Aprobacion WHERE CreditoId = @CreditoId;
    DELETE FROM CREDITO.CreditoImagen WHERE CreditoId = @CreditoId;
    DELETE FROM CREDITO.CentralRiesgo WHERE CreditoId = @CreditoId;
    DELETE FROM CREDITO.Cargo WHERE CreditoId = @CreditoId;

    DELETE st
    FROM CREDITO.Subtarea st
    INNER JOIN CREDITO.Tarea t ON t.TareaId = st.TareaId
    WHERE t.CreditoId = @CreditoId;

    DELETE FROM CREDITO.Tarea WHERE CreditoId = @CreditoId;
    DELETE FROM CREDITO.Prenda WHERE CreditoId = @CreditoId;
    DELETE FROM CREDITO.PlanPago WHERE CreditoId = @CreditoId;
    DELETE FROM CREDITO.Credito WHERE CreditoId = @CreditoId;
END
GO
GO
PRINT N'OK seccion 2026-10-23-rechazar-solicitud-hijos.sql';
GO

GO
/* ===== INICIO 2026-10-23-indices-caminos-calientes.sql ===== */
GO
-- Índices de caminos calientes (rechazo PEN, caja del día, bóveda, PLA).
-- No toca USP ni % . Idempotente. Ventana corta: tablas hijas son chicas
-- salvo CxC (~75k) y BovedaMov (~67k).
-- No indexa las 87 FK de almacén.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Cargo_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.Cargo')
)
    CREATE NONCLUSTERED INDEX IX_Cargo_CreditoId ON CREDITO.Cargo (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Tarea_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.Tarea')
)
    CREATE NONCLUSTERED INDEX IX_Tarea_CreditoId ON CREDITO.Tarea (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CreditoImagen_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.CreditoImagen')
)
    CREATE NONCLUSTERED INDEX IX_CreditoImagen_CreditoId ON CREDITO.CreditoImagen (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CentralRiesgo_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.CentralRiesgo')
)
    CREATE NONCLUSTERED INDEX IX_CentralRiesgo_CreditoId ON CREDITO.CentralRiesgo (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CreditoMora_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.CreditoMora')
)
    CREATE NONCLUSTERED INDEX IX_CreditoMora_CreditoId ON CREDITO.CreditoMora (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CuentaxCobrar_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.CuentaxCobrar')
)
    CREATE NONCLUSTERED INDEX IX_CuentaxCobrar_CreditoId ON CREDITO.CuentaxCobrar (CreditoId)
        INCLUDE (Estado, Monto);
GO

IF OBJECT_ID(N'CREDITO.PlaInusual', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PlaInusual_CreditoId' AND object_id = OBJECT_ID(N'CREDITO.PlaInusual')
)
    CREATE NONCLUSTERED INDEX IX_PlaInusual_CreditoId ON CREDITO.PlaInusual (CreditoId)
        WHERE CreditoId IS NOT NULL;
GO

IF OBJECT_ID(N'CREDITO.PlaInusual', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PlaInusual_Credito'
)
    ALTER TABLE CREDITO.PlaInusual WITH CHECK
        ADD CONSTRAINT FK_PlaInusual_Credito
        FOREIGN KEY (CreditoId) REFERENCES CREDITO.Credito (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CajaDiario_Caja_IndCierre' AND object_id = OBJECT_ID(N'CREDITO.CajaDiario')
)
    CREATE NONCLUSTERED INDEX IX_CajaDiario_Caja_IndCierre
        ON CREDITO.CajaDiario (CajaId, IndCierre)
        INCLUDE (TransBoveda, UsuarioAsignadoId, FechaIniOperacion);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Caja_OficinaId' AND object_id = OBJECT_ID(N'CREDITO.Caja')
)
    CREATE NONCLUSTERED INDEX IX_Caja_OficinaId ON CREDITO.Caja (OficinaId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_BovedaMov_BovedaId' AND object_id = OBJECT_ID(N'CREDITO.BovedaMov')
)
    CREATE NONCLUSTERED INDEX IX_BovedaMov_BovedaId ON CREDITO.BovedaMov (BovedaId, Estado)
        INCLUDE (Importe, IndEntrada, FechaReg);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_BovedaMov_CajaDiarioId' AND object_id = OBJECT_ID(N'CREDITO.BovedaMov')
)
    CREATE NONCLUSTERED INDEX IX_BovedaMov_CajaDiarioId ON CREDITO.BovedaMov (CajaDiarioId)
        WHERE CajaDiarioId IS NOT NULL;
GO

DECLARE @fkRolMenu sysname;
DECLARE @sqlRolMenu nvarchar(400);

SELECT @fkRolMenu = name
FROM sys.foreign_keys
WHERE parent_object_id = OBJECT_ID(N'MAESTRO.RolMenu')
  AND is_not_trusted = 1;

IF @fkRolMenu IS NOT NULL
BEGIN
    SET @sqlRolMenu = N'ALTER TABLE MAESTRO.RolMenu WITH CHECK CHECK CONSTRAINT ' + QUOTENAME(@fkRolMenu);
    EXEC (@sqlRolMenu);
END
GO
GO
PRINT N'OK seccion 2026-10-23-indices-caminos-calientes.sql';
GO

GO
/* ===== INICIO 2026-10-23-indices-consulta-login.sql ===== */
GO
-- Índices de consulta / login / condonación / bóveda / subtarea.
-- Complementa 2026-10-23-indices-caminos-calientes.sql (rechazo/caja).
-- No toca USP ni % . Idempotente. Tablas chicas salvo Credito (~77k).

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Credito_PersonaId' AND object_id = OBJECT_ID(N'CREDITO.Credito')
)
    CREATE NONCLUSTERED INDEX IX_Credito_PersonaId
        ON CREDITO.Credito (PersonaId)
        INCLUDE (Estado, OficinaId);
GO

IF OBJECT_ID(N'CREDITO.CreditoCondonacion', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CreditoCondonacion_CreditoId'
      AND object_id = OBJECT_ID(N'CREDITO.CreditoCondonacion')
)
    CREATE NONCLUSTERED INDEX IX_CreditoCondonacion_CreditoId
        ON CREDITO.CreditoCondonacion (CreditoId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Boveda_OficinaId' AND object_id = OBJECT_ID(N'CREDITO.Boveda')
)
    CREATE NONCLUSTERED INDEX IX_Boveda_OficinaId ON CREDITO.Boveda (OficinaId);
GO

IF OBJECT_ID(N'CREDITO.BovedaCuenta', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_BovedaCuenta_BovedaId'
      AND object_id = OBJECT_ID(N'CREDITO.BovedaCuenta')
)
    CREATE NONCLUSTERED INDEX IX_BovedaCuenta_BovedaId
        ON CREDITO.BovedaCuenta (BovedaId);
GO

IF OBJECT_ID(N'CREDITO.Subtarea', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Subtarea_TareaId' AND object_id = OBJECT_ID(N'CREDITO.Subtarea')
)
    CREATE NONCLUSTERED INDEX IX_Subtarea_TareaId ON CREDITO.Subtarea (TareaId);
GO

IF OBJECT_ID(N'MAESTRO.Cliente', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Cliente_PersonaId' AND object_id = OBJECT_ID(N'MAESTRO.Cliente')
)
    CREATE NONCLUSTERED INDEX IX_Cliente_PersonaId ON MAESTRO.Cliente (PersonaId);
GO

IF OBJECT_ID(N'MAESTRO.UsuarioOficina', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_UsuarioOficina_Usuario_Oficina'
      AND object_id = OBJECT_ID(N'MAESTRO.UsuarioOficina')
)
    CREATE NONCLUSTERED INDEX IX_UsuarioOficina_Usuario_Oficina
        ON MAESTRO.UsuarioOficina (UsuarioId, OficinaId);
GO

IF OBJECT_ID(N'MAESTRO.UsuarioRol', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_UsuarioRol_Usuario_Oficina'
      AND object_id = OBJECT_ID(N'MAESTRO.UsuarioRol')
)
    CREATE NONCLUSTERED INDEX IX_UsuarioRol_Usuario_Oficina
        ON MAESTRO.UsuarioRol (UsuarioId, OficinaId)
        INCLUDE (RolId);
GO
GO
PRINT N'OK seccion 2026-10-23-indices-consulta-login.sql';
GO

GO
/* ===== INICIO 2026-10-23-indices-cobro-anulacion.sql ===== */
GO
-- Índices de cobro / anulación / sesión de caja.
-- Complementa caminos calientes + consulta/login. Idempotente. No toca USP.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CuentaxCobrar_MovimientoCajaId'
      AND object_id = OBJECT_ID(N'CREDITO.CuentaxCobrar')
)
    CREATE NONCLUSTERED INDEX IX_CuentaxCobrar_MovimientoCajaId
        ON CREDITO.CuentaxCobrar (MovimientoCajaId)
        WHERE MovimientoCajaId IS NOT NULL;
GO

IF OBJECT_ID(N'CREDITO.MovimientoCajaAnu', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_MovimientoCajaAnu_MovimientoCajaId'
      AND object_id = OBJECT_ID(N'CREDITO.MovimientoCajaAnu')
)
    CREATE NONCLUSTERED INDEX IX_MovimientoCajaAnu_MovimientoCajaId
        ON CREDITO.MovimientoCajaAnu (MovimientoCajaId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CajaDiario_UsuarioAsignadoId'
      AND object_id = OBJECT_ID(N'CREDITO.CajaDiario')
)
    CREATE NONCLUSTERED INDEX IX_CajaDiario_UsuarioAsignadoId
        ON CREDITO.CajaDiario (UsuarioAsignadoId)
        INCLUDE (IndCierre, CajaId)
        WHERE UsuarioAsignadoId IS NOT NULL;
GO
GO
PRINT N'OK seccion 2026-10-23-indices-cobro-anulacion.sql';
GO

GO
/* ===== INICIO 2026-10-23-indices-pla-desembolso.sql ===== */
GO
-- PLA: desembolsos del mes por sede (usp_PlaCandidatosRo).
-- ~77k filas. No toca MovimientoCaja (1,6 M). Idempotente.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Credito_Oficina_FechaDesembolso'
      AND object_id = OBJECT_ID(N'CREDITO.Credito')
)
    CREATE NONCLUSTERED INDEX IX_Credito_Oficina_FechaDesembolso
        ON CREDITO.Credito (OficinaId, FechaDesembolso)
        INCLUDE (PersonaId, MontoDesembolso, MontoCredito, FechaReg)
        WHERE FechaDesembolso IS NOT NULL;
GO
GO
PRINT N'OK seccion 2026-10-23-indices-pla-desembolso.sql';
GO

GO
/* ===== INICIO 2026-10-24-boveda-cuadre-contrato.sql ===== */
GO
-- Cuadre multisucursal (2026-10-24)
-- 1) usp_TransferirBoveda: @TipoPagoId (default 1) en TRS/TRE; no overdraw del medio.
-- 2) usp_CerrarBoveda: una principal; no cerrar con cajas abiertas / no enviadas.
-- 3) usp_CerrarBovedaTemporal: todos los medios; UsuarioRol ENCARGADO por OficinaId.
-- 4) usp_CerrarCajasDiarios: ROUND 2 decimales (era 1).
-- 5) B-06: UNIQUE una principal abierta por oficina (tabla chica; no 1.6M).
-- No cambia usp_ActualizarSaldosBoveda ni flag 1.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_TransferirBoveda](
@BovedaInicioId INT,
@BovedaDestinoId INT,
@Glosa VARCHAR(MAX),
@Monto DECIMAL(16,2),
@UsuarioRegId INT,
@flagAceptar INT,
@BovedaMovTempId INT=0,
@TipoPagoId SMALLINT=1)
AS
DECLARE @Estado BIT,@FechaReg DATETIME
SET @Estado= 1
SET @FechaReg = dbo.ufnFecha()

IF ISNULL(@TipoPagoId, 0) < 1
	SET @TipoPagoId = 1

IF @flagAceptar = 0
	BEGIN
		IF @BovedaInicioId IS NULL OR @BovedaDestinoId IS NULL
			OR @BovedaInicioId < 1 OR @BovedaDestinoId < 1
		BEGIN
			RAISERROR(N'Origen y destino de bóveda son obligatorios.', 16, 1)
			RETURN
		END

		IF @BovedaInicioId = @BovedaDestinoId
		BEGIN
			RAISERROR(N'La bóveda de origen y destino no pueden ser la misma.', 16, 1)
			RETURN
		END

		IF @Monto IS NULL OR @Monto <= 0
		BEGIN
			RAISERROR(N'El monto debe ser mayor a cero.', 16, 1)
			RETURN
		END

		IF @TipoPagoId NOT BETWEEN 1 AND 5
		BEGIN
			RAISERROR(N'El medio debe ser una marca (1-5). No use tipos de sede (6-8).', 16, 1)
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = @BovedaInicioId AND IndCierre = 0)
		BEGIN
			RAISERROR(N'La bóveda de origen no está abierta.', 16, 1)
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = @BovedaDestinoId AND IndCierre = 0)
		BEGIN
			RAISERROR(N'La bóveda de destino no está abierta.', 16, 1)
			RETURN
		END

		DECLARE @HayCuentas BIT = CASE
			WHEN EXISTS (SELECT 1 FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaInicioId)
			THEN 1 ELSE 0 END
		DECLARE @SiBoveda DECIMAL(16,2) = (
			SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId = @BovedaInicioId)
		DECLARE @SiMedio DECIMAL(16,2) = ISNULL((
			SELECT SaldoInicial
			FROM CREDITO.BovedaCuenta
			WHERE BovedaId = @BovedaInicioId AND TipoPagoId = @TipoPagoId), 0)
		IF @HayCuentas = 0 AND @TipoPagoId = 1
			SET @SiMedio = ISNULL(@SiBoveda, 0)
		DECLARE @NetoMedio DECIMAL(16,2) = ISNULL((
			SELECT SUM(IIF(IndEntrada = 1, Importe, -Importe))
			FROM CREDITO.BovedaMov
			WHERE BovedaId = @BovedaInicioId AND Estado = 1 AND TipoPagoId = @TipoPagoId), 0)

		IF (@SiMedio + @NetoMedio) < @Monto
		BEGIN
			RAISERROR(N'Saldo insuficiente en el medio de origen.', 16, 1)
			RETURN
		END

		INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion,Glosa,Importe,IndEntrada,Estado,UsuarioRegId,FechaReg,TipoPagoId)
					VALUES(@BovedaInicioId, 'TRS',@Glosa, @Monto,0,@Estado,@UsuarioRegId,@FechaReg,@TipoPagoId)
		EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId

		INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion,Glosa,Importe,IndEntrada,Estado,UsuarioRegId,FechaReg,TipoPagoId)
		VALUES(@BovedaDestinoId, 'TRE',@Glosa, @Monto,1,@Estado,@UsuarioRegId,@FechaReg,@TipoPagoId)

		EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaDestinoId
	END

IF @flagAceptar = 2
	BEGIN
	DELETE CREDITO.BovedaMov WHERE MovimientoBovedaId = (SELECT MovimientoBovedaIniId FROM CREDITO.BovedaMovTemp
														 WHERE BovedaMovTempId = @BovedaMovTempId )
	SET @BovedaInicioId = (SELECT BovedaInicioId  FROM CREDITO.BovedaMovTemp WHERE BovedaMovTempId = @BovedaMovTempId)

	EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId

	DELETE CREDITO.BovedaMovTemp where BovedaMovTempId = @BovedaMovTempId
	END
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_CerrarBoveda]
    @OficinaId INT,
	@UsuarioRegId INT
AS
DECLARE @Principales INT = (
	SELECT COUNT(1)
	FROM CREDITO.Boveda
	WHERE OficinaId = @OficinaId AND IndTemporal = 0 AND IndCierre = 0)

IF @Principales = 0
BEGIN
	RAISERROR(N'No hay bóveda principal abierta en la oficina.', 16, 1)
	RETURN
END

IF @Principales > 1
BEGIN
	RAISERROR(N'Hay más de una bóveda principal abierta en la oficina. Corrija antes de cerrar.', 16, 1)
	RETURN
END

IF EXISTS (
	SELECT 1
	FROM CREDITO.CajaDiario AS cd
	INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
	WHERE cd.IndCierre = 0 AND c.OficinaId = @OficinaId)
BEGIN
	RAISERROR(N'EXISTEN CAJAS ABIERTAS.', 16, 1)
	RETURN
END

IF EXISTS (
	SELECT 1
	FROM CREDITO.CajaDiario AS cd
	INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
	WHERE cd.IndCierre = 1 AND cd.TransBoveda = 0 AND c.OficinaId = @OficinaId)
BEGIN
	RAISERROR(N'EXISTEN CAJAS CERRADAS NO ENVIADAS A BOVEDA. REVISE FORMULARIO SALDOS CAJA.', 16, 1)
	RETURN
END

DECLARE @BovedaId INT = (SELECT BovedaId FROM CREDITO.Boveda WHERE OficinaId=@OficinaId AND	IndTemporal=0 AND IndCierre=0)
DECLARE @BovedaNuevoId INT
EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId
DECLARE @SaldoFinal DECIMAL(15,2) = (SELECT SaldoFinal FROM CREDITO.Boveda WHERE BovedaId=@BovedaId)

UPDATE CREDITO.Boveda
SET IndCierre=1, FechaFinOperacion=dbo.ufnFecha()
WHERE BovedaId= @BovedaId

INSERT INTO CREDITO.Boveda
(OficinaId, SaldoInicial, Entradas, Salidas, SaldoFinal, FechaIniOperacion, FechaFinOperacion, IndCierre, IndTemporal)
VALUES (@OficinaId,@SaldoFinal, 0, 0, @SaldoFinal, dbo.ufnFecha(), NULL,0, 0 )

SELECT @BovedaNuevoId=@@IDENTITY

	;WITH CUENTAS AS (
		SELECT MC.BovedaId,MC.TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.Importe,-MC.Importe) ) 'Importe'
		FROM CREDITO.BovedaMov MC
		WHERE MC.BovedaId=@BovedaId
		AND MC.Estado=1
		GROUP BY MC.BovedaId,MC.TipoPagoId
	)
	INSERT	INTO CREDITO.BovedaCuenta
	(BovedaId,TipoPagoId,SaldoInicial)
	SELECT @BovedaNuevoId,vt.ItemId, ISNULL(BC.SaldoInicial,0) + ISNULL(c.Importe,0) 'SaldoFinal'
	FROM MAESTRO.ValorTabla vt
	LEFT JOIN CUENTAS C ON  vt.ItemId=c.TipoPagoId
	LEFT JOIN CREDITO.BovedaCuenta BC ON BC.BovedaId = @BovedaId and BC.TipoPagoId = vt.ItemId
	WHERE vt.TablaId=13 AND vt.ItemId BETWEEN 1 AND 5

	DELETE CREDITO.BovedaCuenta WHERE BovedaId=@BovedaNuevoId AND SaldoInicial=0
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_CerrarBovedaTemporal]
    @OficinaId INT,
	@UsuarioRegId INT
AS
DECLARE @BovedaId INT, @BovedaIdTemporal INT, @ImporteBovedaTemporal DECIMAL(15,2), @RolEncargado INT

SELECT @BovedaIdTemporal=BovedaId, @ImporteBovedaTemporal=SaldoFinal
FROM CREDITO.Boveda
WHERE OficinaId=@OficinaId AND	IndTemporal=1 AND IndCierre=0

SELECT @BovedaId=BovedaId
FROM CREDITO.Boveda
WHERE OficinaId=@OficinaId AND	IndTemporal=0 AND IndCierre=0

IF @BovedaIdTemporal IS NULL
BEGIN
	RAISERROR(N'No hay bóveda temporal abierta en la oficina.', 16, 1)
	RETURN
END

IF @BovedaId IS NULL
BEGIN
	RAISERROR(N'No hay bóveda principal abierta para devolver el saldo temporal.', 16, 1)
	RETURN
END

EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaIdTemporal

;WITH CUENTAS AS (
	SELECT MC.BovedaId,TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.Importe,-MC.Importe) ) 'Importe'
	FROM CREDITO.BovedaMov MC
	WHERE MC.BovedaId=@BovedaIdTemporal
	AND MC.Estado=1
	GROUP BY MC.BovedaId,MC.TipoPagoId
)
INSERT	INTO CREDITO.BovedaMov(BovedaId,CodOperacion,Glosa,TipoPagoId,Importe,IndEntrada, Estado, CajaDiarioId, UsuarioRegId, FechaReg)
SELECT @BovedaId,'TRE','CIERRE BOVEDA TEMPORAL',vt.ItemId, ISNULL(BC.SaldoInicial,0) + ISNULL(c.Importe,0),1,1,0,@UsuarioRegId,dbo.ufnFecha()
FROM MAESTRO.ValorTabla vt
LEFT JOIN CUENTAS C ON vt.ItemId = C.TipoPagoId
LEFT JOIN CREDITO.BovedaCuenta BC ON BC.BovedaId = @BovedaIdTemporal AND BC.TipoPagoId = vt.ItemId
WHERE vt.TablaId=13 AND vt.ItemId BETWEEN 1 AND 5
AND ISNULL(BC.SaldoInicial,0) + ISNULL(c.Importe,0) <> 0

UPDATE CREDITO.Boveda
SET IndCierre=1, FechaFinOperacion=dbo.ufnFecha()
WHERE BovedaId= @BovedaIdTemporal

EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId

SELECT @RolEncargado=r.RolId
FROM MAESTRO.Rol AS r
WHERE r.Denominacion ='ENCARGADO'

IF @RolEncargado IS NOT NULL
	DELETE FROM MAESTRO.UsuarioRol
	WHERE RolId=@RolEncargado AND OficinaId=@OficinaId
GO

CREATE OR ALTER PROC [CREDITO].[usp_CerrarCajasDiarios]
    @UsuarioCierreId INT,
    @OficinaId INT = 1,
    @Sobrante DECIMAL(15, 2) = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @FechaTexto VARCHAR(20) = 'CIERRE ' + CONVERT(varchar, @Fecha, 103) + ' ';

    DECLARE @Principales INT = (
        SELECT COUNT(1)
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = 0
          AND IndTemporal = 0
    );
    IF @Principales > 1
    BEGIN
        RAISERROR(N'Hay más de una bóveda principal abierta en la oficina.', 16, 1);
        RETURN;
    END

    DECLARE @BovedaId INT = (
        SELECT BovedaId
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = 0
          AND IndTemporal = 0
    );

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = 0
          AND IndTemporal = 1
    )
        SET @BovedaId = (
            SELECT BovedaId
            FROM CREDITO.Boveda
            WHERE OficinaId = @OficinaId
              AND IndCierre = 0
              AND IndTemporal = 1
        );

    IF @BovedaId IS NULL
    BEGIN
        RAISERROR(
            N'No se pueden cerrar las cajas: no existe una bóveda activa abierta en la oficina.',
            16,
            1
        );
        RETURN;
    END;

    ;WITH MOVS_BASE AS (
        SELECT
            C.Denominacion AS Caja,
            CD.CajaDiarioId,
            1 AS TipoPagoId,
            CD.SaldoInicial AS Importe
        FROM CREDITO.CajaDiario CD
        INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
        WHERE CD.IndCierre = 1
          AND CD.TransBoveda = 0
          AND C.OficinaId = @OficinaId

        UNION ALL

        SELECT
            C.Denominacion AS Caja,
            CD.CajaDiarioId,
            MC.TipoPagoId,
            SUM(IIF(MC.IndEntrada = 1, MC.ImportePago, -MC.ImportePago)) AS Importe
        FROM CREDITO.MovimientoCaja MC
        INNER JOIN CREDITO.CajaDiario CD ON CD.CajaDiarioId = MC.CajaDiarioId
        INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
        WHERE CD.IndCierre = 1
          AND CD.TransBoveda = 0
          AND C.OficinaId = @OficinaId
          AND MC.Estado = 1
        GROUP BY C.Denominacion, CD.CajaDiarioId, MC.TipoPagoId
    ),
    CUENTAS AS (
        SELECT
            Caja,
            CajaDiarioId,
            TipoPagoId,
            SUM(Importe) AS Importe
        FROM MOVS_BASE
        GROUP BY Caja, CajaDiarioId, TipoPagoId
    )
    INSERT INTO CREDITO.BovedaMov (
        BovedaId,
        CodOperacion,
        Glosa,
        CajaDiarioId,
        TipoPagoId,
        Importe,
        IndEntrada,
        Estado,
        UsuarioRegId,
        FechaReg
    )
    SELECT
        @BovedaId,
        'TRE',
        @FechaTexto + CU.Caja,
        CU.CajaDiarioId,
        CU.TipoPagoId,
        ROUND(CU.Importe, 2),
        1,
        1,
        @UsuarioCierreId,
        @Fecha
    FROM CUENTAS CU
    WHERE ROUND(CU.Importe, 2) <> 0
    ORDER BY CU.CajaDiarioId, CU.TipoPagoId;

    IF @Sobrante > 0
        INSERT INTO CREDITO.BovedaMov (
            BovedaId,
            CodOperacion,
            Glosa,
            CajaDiarioId,
            TipoPagoId,
            Importe,
            IndEntrada,
            Estado,
            UsuarioRegId,
            FechaReg
        )
        VALUES (
            @BovedaId,
            'TRE',
            @FechaTexto + 'SOBRANTE',
            0,
            1,
            @Sobrante,
            1,
            1,
            @UsuarioCierreId,
            @Fecha
        );

    UPDATE CD
    SET CD.TransBoveda = 1
    FROM CREDITO.CajaDiario CD
    INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
    WHERE CD.IndCierre = 1
      AND CD.TransBoveda = 0
      AND C.OficinaId = @OficinaId;

    EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId;
END
GO

IF EXISTS (
    SELECT OficinaId
    FROM CREDITO.Boveda
    WHERE IndCierre = 0 AND IndTemporal = 0
    GROUP BY OficinaId
    HAVING COUNT(1) > 1
)
BEGIN
    RAISERROR(
        N'B-06 no se aplicó: hay más de una bóveda principal abierta en alguna oficina. Corrija y vuelva a ejecutar el índice.',
        16,
        1);
END
ELSE IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Boveda_Oficina_PrincipalAbierta'
      AND object_id = OBJECT_ID(N'CREDITO.Boveda')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Boveda_Oficina_PrincipalAbierta
        ON CREDITO.Boveda (OficinaId)
        WHERE IndCierre = 0 AND IndTemporal = 0;
END
GO
GO
PRINT N'OK seccion 2026-10-24-boveda-cuadre-contrato.sql';
GO

GO
/* ===== INICIO 2026-10-26-prendario-ficha-remate-cobros.sql ===== */
GO
-- 005-facturador-smart · gerencia + DBA 2026-10-26
-- Ficha (observaciones/foto), remate → stock, renovar/liberar prendario.
-- No inventa % de interés: Renovar cobra SUM(PlanPago.Interes) ya calculado.
-- Abonar = usp_PagarCuotaPagoLibre (ya existe). Liberar = cancelación + DEVUELTO.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'CREDITO.Credito', N'FechaRemateEjecutado') IS NULL
    ALTER TABLE CREDITO.Credito ADD FechaRemateEjecutado datetime NULL;

IF COL_LENGTH(N'CREDITO.Prenda', N'ArticuloRemateId') IS NULL
    ALTER TABLE CREDITO.Prenda ADD ArticuloRemateId int NULL;

GO

CREATE OR ALTER PROC [CREDITO].[usp_PrendarioActualizarFicha]
    @PrendaId BIGINT,
    @OficinaId INT,
    @UsuarioId INT,
    @Observaciones NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @PrendaId IS NULL OR @PrendaId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de ficha incompletos.', 16, 1);
        RETURN;
    END;

    UPDATE p
    SET p.Observaciones = NULLIF(LTRIM(RTRIM(@Observaciones)), N''),
        p.UsuarioModId = @UsuarioId,
        p.FechaMod = dbo.ufnFecha()
    FROM CREDITO.Prenda AS p
    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
    WHERE p.PrendaId = @PrendaId
      AND c.OficinaId = @OficinaId
      AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2);

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'Bien no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    SELECT p.PrendaId, p.CreditoId, p.Observaciones, p.FotoPath, p.Estado
    FROM CREDITO.Prenda AS p
    WHERE p.PrendaId = @PrendaId;
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_PrendarioRenovar]
    @CreditoId INT,
    @OficinaId INT,
    @CajaDiarioId INT,
    @UsuarioId INT,
    @Dias INT = 30,
    @TipoPagoId INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreditoId IS NULL OR @CreditoId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @CajaDiarioId IS NULL OR @CajaDiarioId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de renovación incompletos.', 16, 1);
        RETURN;
    END;

    IF @Dias IS NULL OR @Dias < 1
        SET @Dias = 30;
    IF @TipoPagoId IS NULL OR @TipoPagoId < 1
        SET @TipoPagoId = 1;

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.Credito AS c
        WHERE c.CreditoId = @CreditoId
          AND c.OficinaId = @OficinaId
          AND c.Estado = N'DES'
          AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
    )
    BEGIN
        RAISERROR(N'Crédito prendario desembolsado no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.CajaDiario AS cd
        INNER JOIN CREDITO.Caja AS cj ON cj.CajaId = cd.CajaId
        WHERE cd.CajaDiarioId = @CajaDiarioId
          AND cj.OficinaId = @OficinaId
          AND cd.IndCierre = CAST(0 AS bit)
          AND cd.FechaFinOperacion IS NULL
    )
    BEGIN
        RAISERROR(N'La caja no está abierta en esta sede.', 16, 1);
        RETURN;
    END;

    DECLARE @Interes DECIMAL(16, 2) = 0;
    DECLARE @PersonaId INT;
    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @MovimientoCajaId INT;

    SELECT @Interes = ISNULL(SUM(pp.Interes), 0)
    FROM CREDITO.PlanPago AS pp
    WHERE pp.CreditoId = @CreditoId
      AND pp.Estado = N'PEN';

    SELECT @PersonaId = c.PersonaId
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;

    IF @Interes > 0
    BEGIN
        INSERT INTO CREDITO.MovimientoCaja (
            CajaDiarioId, PersonaId, Operacion, ImportePago,
            Descripcion, IndEntrada, Estado, OrdenVentaId, CreditoId,
            UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (
            @CajaDiarioId, @PersonaId, N'CUO', @Interes,
            N'RENOVACION PRENDARIA INTERES CREDITO ' + CAST(@CreditoId AS varchar(20)),
            1, 1, NULL, @CreditoId, @UsuarioId, @Fecha, @TipoPagoId);

        SET @MovimientoCajaId = CONVERT(INT, SCOPE_IDENTITY());
        EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId = @CajaDiarioId;
    END;

    DECLARE @NuevaRemate DATE;
    SELECT @NuevaRemate = DATEADD(
        DAY,
        @Dias,
        COALESCE(c.FechaRemate, DATEADD(DAY, 30, c.FechaVencimiento)))
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;

    EXEC CREDITO.usp_ProrrogarCredito @CreditoId = @CreditoId, @Dias = @Dias;

    UPDATE CREDITO.Credito
    SET FechaRemate = @NuevaRemate,
        UsuarioModId = @UsuarioId,
        FechaMod = @Fecha
    WHERE CreditoId = @CreditoId
      AND OficinaId = @OficinaId;

    SELECT
        @CreditoId AS CreditoId,
        @Interes AS InteresCobrado,
        @Dias AS Dias,
        @MovimientoCajaId AS MovimientoCajaId,
        c.FechaVencimiento,
        c.FechaRemate
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_PrendarioLiberar]
    @CreditoId INT,
    @OficinaId INT,
    @CajaDiarioId INT,
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreditoId IS NULL OR @CreditoId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @CajaDiarioId IS NULL OR @CajaDiarioId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de liberación incompletos.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.Credito AS c
        WHERE c.CreditoId = @CreditoId
          AND c.OficinaId = @OficinaId
          AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
    )
    BEGIN
        RAISERROR(N'Crédito prendario no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.CajaDiario AS cd
        INNER JOIN CREDITO.Caja AS cj ON cj.CajaId = cd.CajaId
        WHERE cd.CajaDiarioId = @CajaDiarioId
          AND cj.OficinaId = @OficinaId
          AND cd.IndCierre = CAST(0 AS bit)
          AND cd.FechaFinOperacion IS NULL
    )
    BEGIN
        RAISERROR(N'La caja no está abierta en esta sede.', 16, 1);
        RETURN;
    END;

    DECLARE @Fecha DATETIME = dbo.ufnFecha();

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
          AND Estado = N'DES'
    )
    BEGIN
        EXEC CREDITO.usp_PagarCuotasCancelacion
            @CajaDiarioId = @CajaDiarioId,
            @CreditoId = @CreditoId,
            @UsuarioId = @UsuarioId,
            @FechaPago = NULL;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito AS c
        INNER JOIN CREDITO.PlanPago AS pp ON pp.CreditoId = c.CreditoId
        WHERE c.CreditoId = @CreditoId
          AND c.Estado = N'DES'
          AND pp.Estado = N'PEN'
    )
    BEGIN
        RAISERROR(N'Quedan cuotas pendientes. No se liberó la prenda.', 16, 1);
        RETURN;
    END;

    UPDATE p
    SET p.Estado = N'DEVUELTO',
        p.UsuarioModId = @UsuarioId,
        p.FechaMod = @Fecha
    FROM CREDITO.Prenda AS p
    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
    WHERE p.CreditoId = @CreditoId
      AND c.OficinaId = @OficinaId
      AND p.Estado = N'EN CUSTODIA';

    SELECT
        @CreditoId AS CreditoId,
        c.Estado,
        (SELECT COUNT(*) FROM CREDITO.Prenda WHERE CreditoId = @CreditoId AND Estado = N'DEVUELTO') AS BienesDevueltos
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_RematarPrenda]
    @PrendaId BIGINT,
    @OficinaId INT,
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @PrendaId IS NULL OR @PrendaId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de remate incompletos.', 16, 1);
        RETURN;
    END;

    DECLARE @Hoy DATE = CONVERT(date, dbo.ufnFecha());
    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @CreditoId INT;
    DECLARE @Descripcion NVARCHAR(500);
    DECLARE @Serie NVARCHAR(100);
    DECLARE @Tasacion DECIMAL(18, 2);
    DECLARE @FotoPath NVARCHAR(500);
    DECLARE @AlmacenId INT;
    DECLARE @ArticuloId INT;
    DECLARE @CodArticulo VARCHAR(20);

    SELECT
        @CreditoId = c.CreditoId,
        @Descripcion = p.Descripcion,
        @Serie = p.Serie,
        @Tasacion = p.ValorTasacion,
        @FotoPath = p.FotoPath
    FROM CREDITO.Prenda AS p
    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
    WHERE p.PrendaId = @PrendaId
      AND c.OficinaId = @OficinaId
      AND c.Estado = N'DES'
      AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
      AND p.Estado = N'EN CUSTODIA'
      AND c.FechaRemate IS NOT NULL
      AND c.FechaRemate < @Hoy;

    IF @CreditoId IS NULL
    BEGIN
        RAISERROR(N'Solo se remata un bien en custodia de un crédito en cola de remate (vencimiento + 30 d.).', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
          AND FechaRemateEjecutado IS NOT NULL
    )
    BEGIN
        RAISERROR(N'Este crédito ya fue rematado.', 16, 1);
        RETURN;
    END;

    SELECT TOP (1) @AlmacenId = a.AlmacenId
    FROM ALMACEN.Almacen AS a
    WHERE a.OficinaId = @OficinaId
      AND a.Estado = CAST(1 AS bit)
    ORDER BY a.AlmacenId;

    IF @AlmacenId IS NULL
    BEGIN
        RAISERROR(N'La sede no tiene almacén activo. Cree el almacén antes de rematar.', 16, 1);
        RETURN;
    END;

    SET @CodArticulo = LEFT('PRM' + CAST(@PrendaId AS varchar(16)), 20);

    INSERT INTO ALMACEN.Articulo (
        ModeloId, TipoArticuloId, CodArticulo, Denominacion, Descripcion,
        Imagen, IndPerecible, IndImportado, Estado)
    VALUES (
        NULL, NULL, @CodArticulo,
        LEFT(@Descripcion, 200),
        LEFT(CONCAT(N'Remate crédito ', @CreditoId, N' prenda ', @PrendaId), 250),
        @FotoPath, 0, 0, 1);

    SET @ArticuloId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO VENTAS.ListaPrecio (ArticuloId, Monto, Descuento, Estado, Puntos, PuntosCanje)
    VALUES (@ArticuloId, @Tasacion, 0, 1, 0, 0);

    INSERT INTO ALMACEN.SerieArticulo (
        NumeroSerie, AlmacenId, ArticuloId, EstadoId, MovimientoDetEntId, MovimientoDetSalId)
    VALUES (
        LEFT(COALESCE(NULLIF(LTRIM(RTRIM(@Serie)), N''), N'PRM' + CAST(@PrendaId AS varchar(16))), 20),
        @AlmacenId,
        @ArticuloId,
        2,
        NULL,
        NULL);

    UPDATE CREDITO.Prenda
    SET Estado = N'REMATADO',
        ArticuloRemateId = @ArticuloId,
        UsuarioModId = @UsuarioId,
        FechaMod = @Fecha
    WHERE PrendaId = @PrendaId
      AND Estado = N'EN CUSTODIA';

    UPDATE CREDITO.Credito
    SET FechaRemateEjecutado = @Fecha,
        IndIrrecuperable = CAST(1 AS bit),
        UsuarioModId = @UsuarioId,
        FechaMod = @Fecha
    WHERE CreditoId = @CreditoId
      AND OficinaId = @OficinaId;

    SELECT
        @PrendaId AS PrendaId,
        @CreditoId AS CreditoId,
        @ArticuloId AS ArticuloId,
        @CodArticulo AS CodArticulo,
        @Tasacion AS PrecioVenta,
        @AlmacenId AS AlmacenId;
END
GO

-- US-5: si el menú de venta rápida / almacén ya existe, se concede a operación.
INSERT INTO MAESTRO.RolMenu (RolId, MenuId)
SELECT r.RolId, m.MenuId
FROM MAESTRO.Rol AS r
CROSS JOIN MAESTRO.Menu AS m
WHERE r.Denominacion IN (N'ANALISTA', N'CAJERO', N'ADMINISTRADOR', N'GESTOR')
  AND (
        m.Url LIKE N'%VentaRapida%'
     OR m.Url LIKE N'%OrdenVenta%'
     OR m.Denominacion IN (N'VENTAS')
     OR (m.Modulo LIKE N'%ALMACEN%' AND m.IndPadre = 0)
     OR m.Url LIKE N'%Almacen/Index%'
  )
  AND NOT EXISTS (
        SELECT 1
        FROM MAESTRO.RolMenu AS rm
        WHERE rm.RolId = r.RolId
          AND rm.MenuId = m.MenuId
  );
GO
GO
PRINT N'OK seccion 2026-10-26-prendario-ficha-remate-cobros.sql';
GO

GO
/* ===== INICIO 2026-10-26-prendario-ciclo-cierre.sql ===== */
GO
-- 005-facturador-smart · cierre 100 % (re-ejecutable)
-- Actualiza USP de cobro/remate, cola FechaRemate, menú venta rápida y almacén por sede.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'CREDITO.Credito', N'FechaRemateEjecutado') IS NULL
    ALTER TABLE CREDITO.Credito ADD FechaRemateEjecutado datetime NULL;

IF COL_LENGTH(N'CREDITO.Prenda', N'ArticuloRemateId') IS NULL
    ALTER TABLE CREDITO.Prenda ADD ArticuloRemateId int NULL;
GO

UPDATE CREDITO.Credito
SET FechaRemate = DATEADD(DAY, 30, FechaVencimiento)
WHERE FechaRemate IS NULL
  AND FechaVencimiento IS NOT NULL
  AND Estado = N'DES'
  AND (EsPrendario = CAST(1 AS bit) OR ProductoId = 2);
GO

INSERT INTO ALMACEN.Almacen (OficinaId, Denominacion, Descripcion, IndEstadoApertura, FechaApertura, Estado)
SELECT
    o.OficinaId,
    LEFT(CONCAT(N'ALMACEN ', ISNULL(o.Denominacion, CAST(o.OficinaId AS varchar(8)))), 100),
    N'Almacén para remate prendario',
    1,
    dbo.ufnFecha(),
    1
FROM MAESTRO.Oficina AS o
WHERE o.Estado = CAST(1 AS bit)
  AND NOT EXISTS (
        SELECT 1
        FROM ALMACEN.Almacen AS a
        WHERE a.OficinaId = o.OficinaId
          AND a.Estado = CAST(1 AS bit)
  );
GO

DECLARE @OrdenVentas DECIMAL(3, 1);
SELECT TOP (1) @OrdenVentas = Orden
FROM MAESTRO.Menu
WHERE Denominacion = N'VENTAS' AND IndPadre = 1
ORDER BY MenuId;

IF @OrdenVentas IS NULL
    SET @OrdenVentas = 4.0;

IF NOT EXISTS (
    SELECT 1 FROM MAESTRO.Menu
    WHERE Url LIKE N'%VentaRapida%' OR Denominacion = N'VENTA RAPIDA'
)
BEGIN
    INSERT INTO MAESTRO.Menu (Denominacion, Modulo, Url, Icono, IndPadre, Orden, Referencia)
    VALUES (
        N'VENTA RAPIDA',
        N'VENTAS',
        N'VentaRapida',
        N'icon-list',
        0,
        @OrdenVentas + 0.1,
        @OrdenVentas
    );
END
GO


CREATE OR ALTER PROC [CREDITO].[usp_PrendarioActualizarFicha]
    @PrendaId BIGINT,
    @OficinaId INT,
    @UsuarioId INT,
    @Observaciones NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @PrendaId IS NULL OR @PrendaId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de ficha incompletos.', 16, 1);
        RETURN;
    END;

    UPDATE p
    SET p.Observaciones = NULLIF(LTRIM(RTRIM(@Observaciones)), N''),
        p.UsuarioModId = @UsuarioId,
        p.FechaMod = dbo.ufnFecha()
    FROM CREDITO.Prenda AS p
    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
    WHERE p.PrendaId = @PrendaId
      AND c.OficinaId = @OficinaId
      AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2);

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'Bien no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    SELECT p.PrendaId, p.CreditoId, p.Observaciones, p.FotoPath, p.Estado
    FROM CREDITO.Prenda AS p
    WHERE p.PrendaId = @PrendaId;
END


GO

CREATE OR ALTER PROC [CREDITO].[usp_PrendarioRenovar]
    @CreditoId INT,
    @OficinaId INT,
    @CajaDiarioId INT,
    @UsuarioId INT,
    @Dias INT = 30,
    @TipoPagoId INT = 1,
    @FechaPagoTransferencia VARCHAR(16) = ''
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreditoId IS NULL OR @CreditoId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @CajaDiarioId IS NULL OR @CajaDiarioId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de renovaci�n incompletos.', 16, 1);
        RETURN;
    END;

    IF @Dias IS NULL OR @Dias < 1
        SET @Dias = 30;
    IF @TipoPagoId IS NULL OR @TipoPagoId < 1
        SET @TipoPagoId = 1;
    IF @FechaPagoTransferencia IS NULL
        SET @FechaPagoTransferencia = '';

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.Credito AS c
        WHERE c.CreditoId = @CreditoId
          AND c.OficinaId = @OficinaId
          AND c.Estado = N'DES'
          AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
    )
    BEGIN
        RAISERROR(N'Cr�dito prendario desembolsado no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.CajaDiario AS cd
        INNER JOIN CREDITO.Caja AS cj ON cj.CajaId = cd.CajaId
        WHERE cd.CajaDiarioId = @CajaDiarioId
          AND cj.OficinaId = @OficinaId
          AND cd.IndCierre = CAST(0 AS bit)
          AND cd.FechaFinOperacion IS NULL
    )
    BEGIN
        RAISERROR(N'La caja no est� abierta en esta sede.', 16, 1);
        RETURN;
    END;

    DECLARE @Interes DECIMAL(16, 2) = 0;
    DECLARE @PersonaId INT;
    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @MovimientoCajaId INT;

    SELECT @Interes = ISNULL(SUM(pp.Interes), 0)
    FROM CREDITO.PlanPago AS pp
    WHERE pp.CreditoId = @CreditoId
      AND pp.Estado = N'PEN';

    SELECT @PersonaId = c.PersonaId
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;

    IF @Interes > 0
    BEGIN
        INSERT INTO CREDITO.MovimientoCaja (
            CajaDiarioId, PersonaId, Operacion, ImportePago,
            Descripcion, IndEntrada, Estado, OrdenVentaId, CreditoId,
            UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (
            @CajaDiarioId, @PersonaId, N'CUO', @Interes,
            N'RENOVACION PRENDARIA INTERES CREDITO ' + CAST(@CreditoId AS varchar(20)),
            1, 1, NULL, @CreditoId, @UsuarioId, @Fecha, @TipoPagoId);

        SET @MovimientoCajaId = CONVERT(INT, SCOPE_IDENTITY());

        IF @TipoPagoId > 1
            INSERT INTO CREDITO.MovimientoCajaExtension (
                MovimientoCajaId, FechaTransferencia, IndTransferenciaVerificada)
            VALUES (@MovimientoCajaId, @FechaPagoTransferencia, 0);

        EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId = @CajaDiarioId;
    END;

    DECLARE @NuevaRemate DATE;
    SELECT @NuevaRemate = DATEADD(
        DAY,
        @Dias,
        COALESCE(c.FechaRemate, DATEADD(DAY, 30, c.FechaVencimiento)))
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;

    EXEC CREDITO.usp_ProrrogarCredito @CreditoId = @CreditoId, @Dias = @Dias;

    UPDATE CREDITO.Credito
    SET FechaRemate = @NuevaRemate,
        UsuarioModId = @UsuarioId,
        FechaMod = @Fecha
    WHERE CreditoId = @CreditoId
      AND OficinaId = @OficinaId;

    SELECT
        @CreditoId AS CreditoId,
        @Interes AS InteresCobrado,
        @Dias AS Dias,
        @MovimientoCajaId AS MovimientoCajaId,
        c.FechaVencimiento,
        c.FechaRemate
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;
END


GO

CREATE OR ALTER PROC [CREDITO].[usp_PrendarioLiberar]
    @CreditoId INT,
    @OficinaId INT,
    @CajaDiarioId INT,
    @UsuarioId INT,
    @TipoPagoId INT = 1,
    @FechaPagoTransferencia VARCHAR(16) = ''
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreditoId IS NULL OR @CreditoId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @CajaDiarioId IS NULL OR @CajaDiarioId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de liberaci�n incompletos.', 16, 1);
        RETURN;
    END;

    IF @TipoPagoId IS NULL OR @TipoPagoId < 1
        SET @TipoPagoId = 1;
    IF @FechaPagoTransferencia IS NULL
        SET @FechaPagoTransferencia = '';

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.Credito AS c
        WHERE c.CreditoId = @CreditoId
          AND c.OficinaId = @OficinaId
          AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
    )
    BEGIN
        RAISERROR(N'Cr�dito prendario no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.CajaDiario AS cd
        INNER JOIN CREDITO.Caja AS cj ON cj.CajaId = cd.CajaId
        WHERE cd.CajaDiarioId = @CajaDiarioId
          AND cj.OficinaId = @OficinaId
          AND cd.IndCierre = CAST(0 AS bit)
          AND cd.FechaFinOperacion IS NULL
    )
    BEGIN
        RAISERROR(N'La caja no est� abierta en esta sede.', 16, 1);
        RETURN;
    END;

    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @ListaPlanPagoId VARCHAR(MAX) = '';
    DECLARE @SumaPagoCuota DECIMAL(16, 2) = 0;
    DECLARE @NroCuotas INT = 0;
    DECLARE @Index INT = 1;
    DECLARE @PagoCuota DECIMAL(16, 2) = 0;
    DECLARE @PlanPagoId INT;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
          AND Estado = N'DES'
    )
    BEGIN
        DECLARE @tCuotasPendientes TABLE (
            Id INT IDENTITY(1, 1),
            PlanPagoId INT,
            Glosa VARCHAR(MAX),
            FechaVencimiento DATE,
            Amortizacion DECIMAL(16, 2),
            Interes DECIMAL(16, 2),
            GastosAdm DECIMAL(16, 2),
            Cuota DECIMAL(16, 2),
            DiasAtrazo INT,
            ImporteMora DECIMAL(16, 2),
            Descuento DECIMAL(16, 2),
            Cargo DECIMAL(16, 2),
            PagoLibre DECIMAL(16, 2),
            PagoCuota DECIMAL(16, 2)
        );

        INSERT INTO @tCuotasPendientes
        EXEC CREDITO.usp_CuotasPendientes @CreditoId, @Fecha, 1;

        SET @NroCuotas = (SELECT COUNT(1) FROM @tCuotasPendientes);
        WHILE @Index <= @NroCuotas
        BEGIN
            SELECT @PagoCuota = PagoCuota, @PlanPagoId = PlanPagoId
            FROM @tCuotasPendientes
            WHERE Id = @Index;

            SET @SumaPagoCuota = @SumaPagoCuota + @PagoCuota;
            SET @ListaPlanPagoId = @ListaPlanPagoId + CAST(@PlanPagoId AS varchar(10)) + ',';
            SET @Index = @Index + 1;
        END;

        IF LEN(@ListaPlanPagoId) > 0
        BEGIN
            SET @ListaPlanPagoId = SUBSTRING(@ListaPlanPagoId, 1, LEN(@ListaPlanPagoId) - 1);
            EXEC CREDITO.usp_PagarCuotas
                @CajaDiarioId = @CajaDiarioId,
                @CreditoId = @CreditoId,
                @ListaPlanPagoId = @ListaPlanPagoId,
                @ImporteRecibido = @SumaPagoCuota,
                @UsuarioId = @UsuarioId,
                @FechaPago = NULL,
                @TipoPagoId = @TipoPagoId,
                @FechaPagoTransferencia = @FechaPagoTransferencia;
        END;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito AS c
        INNER JOIN CREDITO.PlanPago AS pp ON pp.CreditoId = c.CreditoId
        WHERE c.CreditoId = @CreditoId
          AND c.Estado = N'DES'
          AND pp.Estado = N'PEN'
    )
    BEGIN
        RAISERROR(N'Quedan cuotas pendientes. No se liber� la prenda.', 16, 1);
        RETURN;
    END;

    UPDATE p
    SET p.Estado = N'DEVUELTO',
        p.UsuarioModId = @UsuarioId,
        p.FechaMod = @Fecha
    FROM CREDITO.Prenda AS p
    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
    WHERE p.CreditoId = @CreditoId
      AND c.OficinaId = @OficinaId
      AND p.Estado = N'EN CUSTODIA';

    SELECT
        @CreditoId AS CreditoId,
        c.Estado,
        (SELECT COUNT(*) FROM CREDITO.Prenda WHERE CreditoId = @CreditoId AND Estado = N'DEVUELTO') AS BienesDevueltos
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;
END


GO

CREATE OR ALTER PROC [CREDITO].[usp_RematarPrenda]
    @PrendaId BIGINT,
    @OficinaId INT,
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @PrendaId IS NULL OR @PrendaId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de remate incompletos.', 16, 1);
        RETURN;
    END;

    DECLARE @Hoy DATE = CONVERT(date, dbo.ufnFecha());
    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @CreditoId INT;
    DECLARE @AlmacenId INT;
    DECLARE @ArticuloId INT;
    DECLARE @CodArticulo VARCHAR(20);
    DECLARE @TasacionSolicitada DECIMAL(18, 2);

    SELECT
        @CreditoId = c.CreditoId,
        @TasacionSolicitada = p.ValorTasacion
    FROM CREDITO.Prenda AS p
    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
    WHERE p.PrendaId = @PrendaId
      AND c.OficinaId = @OficinaId
      AND c.Estado = N'DES'
      AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
      AND p.Estado = N'EN CUSTODIA'
      AND (
            (c.FechaRemate IS NOT NULL AND c.FechaRemate < @Hoy)
         OR (c.FechaRemate IS NULL AND DATEADD(DAY, 30, c.FechaVencimiento) < @Hoy)
      );

    IF @CreditoId IS NULL
    BEGIN
        RAISERROR(N'Solo se remata un bien en custodia de un cr�dito en cola de remate (vencimiento + 30 d.).', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
          AND FechaRemateEjecutado IS NOT NULL
    )
    BEGIN
        RAISERROR(N'Este cr�dito ya fue rematado.', 16, 1);
        RETURN;
    END;

    SELECT TOP (1) @AlmacenId = a.AlmacenId
    FROM ALMACEN.Almacen AS a
    WHERE a.OficinaId = @OficinaId
      AND a.Estado = CAST(1 AS bit)
    ORDER BY a.AlmacenId;

    IF @AlmacenId IS NULL
    BEGIN
        RAISERROR(N'La sede no tiene almac�n activo. Cree el almac�n antes de rematar.', 16, 1);
        RETURN;
    END;

    DECLARE @CurPrendaId BIGINT;
    DECLARE @Descripcion NVARCHAR(500);
    DECLARE @Serie NVARCHAR(100);
    DECLARE @Tasacion DECIMAL(18, 2);
    DECLARE @FotoPath NVARCHAR(500);
    DECLARE @ArticuloPedido INT;

    WHILE EXISTS (
        SELECT 1
        FROM CREDITO.Prenda
        WHERE CreditoId = @CreditoId
          AND Estado = N'EN CUSTODIA'
    )
    BEGIN
        SELECT TOP (1)
            @CurPrendaId = PrendaId,
            @Descripcion = Descripcion,
            @Serie = Serie,
            @Tasacion = ValorTasacion,
            @FotoPath = FotoPath
        FROM CREDITO.Prenda
        WHERE CreditoId = @CreditoId
          AND Estado = N'EN CUSTODIA'
        ORDER BY CASE WHEN PrendaId = @PrendaId THEN 0 ELSE 1 END, PrendaId;

        SET @CodArticulo = LEFT('PRM' + CAST(@CurPrendaId AS varchar(16)), 20);

        INSERT INTO ALMACEN.Articulo (
            ModeloId, TipoArticuloId, CodArticulo, Denominacion, Descripcion,
            Imagen, IndPerecible, IndImportado, Estado)
        VALUES (
            NULL, NULL, @CodArticulo,
            LEFT(@Descripcion, 200),
            LEFT(CONCAT(N'Remate cr�dito ', @CreditoId, N' prenda ', @CurPrendaId), 250),
            @FotoPath, 0, 0, 1);

        SET @ArticuloId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT INTO VENTAS.ListaPrecio (ArticuloId, Monto, Descuento, Estado, Puntos, PuntosCanje)
        VALUES (@ArticuloId, @Tasacion, 0, 1, 0, 0);

        INSERT INTO ALMACEN.SerieArticulo (
            NumeroSerie, AlmacenId, ArticuloId, EstadoId, MovimientoDetEntId, MovimientoDetSalId)
        VALUES (
            LEFT(COALESCE(NULLIF(LTRIM(RTRIM(@Serie)), N''), N'PRM' + CAST(@CurPrendaId AS varchar(16))), 20),
            @AlmacenId,
            @ArticuloId,
            2,
            NULL,
            NULL);

        UPDATE CREDITO.Prenda
        SET Estado = N'REMATADO',
            ArticuloRemateId = @ArticuloId,
            UsuarioModId = @UsuarioId,
            FechaMod = @Fecha
        WHERE PrendaId = @CurPrendaId
          AND Estado = N'EN CUSTODIA';

        IF @CurPrendaId = @PrendaId
            SET @ArticuloPedido = @ArticuloId;
    END;

    UPDATE CREDITO.Credito
    SET FechaRemateEjecutado = @Fecha,
        FechaRemate = COALESCE(FechaRemate, DATEADD(DAY, 30, FechaVencimiento)),
        IndIrrecuperable = CAST(1 AS bit),
        UsuarioModId = @UsuarioId,
        FechaMod = @Fecha
    WHERE CreditoId = @CreditoId
      AND OficinaId = @OficinaId;

    SELECT
        @PrendaId AS PrendaId,
        @CreditoId AS CreditoId,
        @ArticuloPedido AS ArticuloId,
        LEFT('PRM' + CAST(@PrendaId AS varchar(16)), 20) AS CodArticulo,
        @TasacionSolicitada AS PrecioVenta,
        @AlmacenId AS AlmacenId;
END


GO


INSERT INTO MAESTRO.RolMenu (RolId, MenuId)
SELECT r.RolId, m.MenuId
FROM MAESTRO.Rol AS r
CROSS JOIN MAESTRO.Menu AS m
WHERE r.Denominacion IN (N'ANALISTA', N'CAJERO', N'ADMINISTRADOR', N'GESTOR')
  AND (
        m.Url LIKE N'%VentaRapida%'
     OR m.Url LIKE N'%OrdenVenta%'
     OR m.Denominacion IN (N'VENTAS', N'VENTA RAPIDA')
     OR (m.Modulo LIKE N'%ALMACEN%' AND m.IndPadre = 0)
     OR m.Url LIKE N'%Almacen/Index%'
  )
  AND NOT EXISTS (
        SELECT 1 FROM MAESTRO.RolMenu AS rm
        WHERE rm.RolId = r.RolId AND rm.MenuId = m.MenuId
  );
GO
GO
PRINT N'OK seccion 2026-10-26-prendario-ciclo-cierre.sql';
GO

GO
/* ===== INICIO 2026-10-27-prendario-remate-cola.sql ===== */
GO
-- Alinea renovar: FechaRemate se calcula con el vencimiento actual
-- (antes de prorrogar). Si FechaRemate es NULL y se prorroga primero,
-- el UPDATE sumaba @Dias otra vez sobre vencimiento+30 ya corrido.
-- Re-ejecutable. Listado/KPI de cola de remate van en el API (COALESCE).

SET NOCOUNT ON;
GO

CREATE OR ALTER PROC [CREDITO].[usp_PrendarioRenovar]
    @CreditoId INT,
    @OficinaId INT,
    @CajaDiarioId INT,
    @UsuarioId INT,
    @Dias INT = 30,
    @TipoPagoId INT = 1,
    @FechaPagoTransferencia VARCHAR(16) = ''
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreditoId IS NULL OR @CreditoId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @CajaDiarioId IS NULL OR @CajaDiarioId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de renovación incompletos.', 16, 1);
        RETURN;
    END;

    IF @Dias IS NULL OR @Dias < 1
        SET @Dias = 30;
    IF @TipoPagoId IS NULL OR @TipoPagoId < 1
        SET @TipoPagoId = 1;
    IF @FechaPagoTransferencia IS NULL
        SET @FechaPagoTransferencia = '';

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.Credito AS c
        WHERE c.CreditoId = @CreditoId
          AND c.OficinaId = @OficinaId
          AND c.Estado = N'DES'
          AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
    )
    BEGIN
        RAISERROR(N'Crédito prendario desembolsado no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.CajaDiario AS cd
        INNER JOIN CREDITO.Caja AS cj ON cj.CajaId = cd.CajaId
        WHERE cd.CajaDiarioId = @CajaDiarioId
          AND cj.OficinaId = @OficinaId
          AND cd.IndCierre = CAST(0 AS bit)
          AND cd.FechaFinOperacion IS NULL
    )
    BEGIN
        RAISERROR(N'La caja no está abierta en esta sede.', 16, 1);
        RETURN;
    END;

    DECLARE @Interes DECIMAL(16, 2) = 0;
    DECLARE @PersonaId INT;
    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @MovimientoCajaId INT;
    DECLARE @NuevaRemate DATE;

    SELECT @Interes = ISNULL(SUM(pp.Interes), 0)
    FROM CREDITO.PlanPago AS pp
    WHERE pp.CreditoId = @CreditoId
      AND pp.Estado = N'PEN';

    SELECT @PersonaId = c.PersonaId
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;

    IF @Interes > 0
    BEGIN
        INSERT INTO CREDITO.MovimientoCaja (
            CajaDiarioId, PersonaId, Operacion, ImportePago,
            Descripcion, IndEntrada, Estado, OrdenVentaId, CreditoId,
            UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (
            @CajaDiarioId, @PersonaId, N'CUO', @Interes,
            N'RENOVACION PRENDARIA INTERES CREDITO ' + CAST(@CreditoId AS varchar(20)),
            1, 1, NULL, @CreditoId, @UsuarioId, @Fecha, @TipoPagoId);

        SET @MovimientoCajaId = CONVERT(INT, SCOPE_IDENTITY());

        IF @TipoPagoId > 1
            INSERT INTO CREDITO.MovimientoCajaExtension (
                MovimientoCajaId, FechaTransferencia, IndTransferenciaVerificada)
            VALUES (@MovimientoCajaId, @FechaPagoTransferencia, 0);

        EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId = @CajaDiarioId;
    END;

    SELECT @NuevaRemate = DATEADD(
        DAY,
        @Dias,
        COALESCE(c.FechaRemate, DATEADD(DAY, 30, c.FechaVencimiento)))
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;

    EXEC CREDITO.usp_ProrrogarCredito @CreditoId = @CreditoId, @Dias = @Dias;

    UPDATE CREDITO.Credito
    SET FechaRemate = @NuevaRemate,
        UsuarioModId = @UsuarioId,
        FechaMod = @Fecha
    WHERE CreditoId = @CreditoId
      AND OficinaId = @OficinaId;

    SELECT
        @CreditoId AS CreditoId,
        @Interes AS InteresCobrado,
        @Dias AS Dias,
        @MovimientoCajaId AS MovimientoCajaId,
        c.FechaVencimiento,
        c.FechaRemate
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;
END
GO
GO
PRINT N'OK seccion 2026-10-27-prendario-remate-cola.sql';
GO

GO
/* ===== INICIO 2026-10-27-remate-catalogo-general.sql ===== */
GO
-- Remate: si no hay tipo/modelo, usa o crea GENERAL (marca/tipo/modelo).
-- El bien sale vendible sin armar catálogo a mano. Re-ejecutable.
-- Fuente: db/schema/routines/procedures/CREDITO.usp_RematarPrenda.sql

SET NOCOUNT ON;
GO
CREATE OR ALTER PROC [CREDITO].[usp_RematarPrenda]
    @PrendaId BIGINT,
    @OficinaId INT,
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @PrendaId IS NULL OR @PrendaId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de remate incompletos.', 16, 1);
        RETURN;
    END;

    DECLARE @Hoy DATE = CONVERT(date, dbo.ufnFecha());
    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @CreditoId INT;
    DECLARE @AlmacenId INT;
    DECLARE @ArticuloId INT;
    DECLARE @CodArticulo VARCHAR(20);
    DECLARE @TasacionSolicitada DECIMAL(18, 2);

    SELECT
        @CreditoId = c.CreditoId,
        @TasacionSolicitada = p.ValorTasacion
    FROM CREDITO.Prenda AS p
    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
    WHERE p.PrendaId = @PrendaId
      AND c.OficinaId = @OficinaId
      AND c.Estado = N'DES'
      AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
      AND p.Estado = N'EN CUSTODIA'
      AND (
            (c.FechaRemate IS NOT NULL AND c.FechaRemate < @Hoy)
         OR (c.FechaRemate IS NULL AND DATEADD(DAY, 30, c.FechaVencimiento) < @Hoy)
      );

    IF @CreditoId IS NULL
    BEGIN
        RAISERROR(N'Solo se remata un bien en custodia de un crédito en cola de remate (vencimiento + 30 d.).', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
          AND FechaRemateEjecutado IS NOT NULL
    )
    BEGIN
        RAISERROR(N'Este crédito ya fue rematado.', 16, 1);
        RETURN;
    END;

    SELECT TOP (1) @AlmacenId = a.AlmacenId
    FROM ALMACEN.Almacen AS a
    WHERE a.OficinaId = @OficinaId
      AND a.Estado = CAST(1 AS bit)
    ORDER BY a.AlmacenId;

    IF @AlmacenId IS NULL
    BEGIN
        RAISERROR(N'La sede no tiene almacén activo. Cree el almacén antes de rematar.', 16, 1);
        RETURN;
    END;

    DECLARE @TipoId INT;
    DECLARE @ModeloId INT;
    DECLARE @MarcaId INT;

    SELECT TOP (1) @TipoId = TipoArticuloId
    FROM ALMACEN.TipoArticulo
    WHERE Estado = CAST(1 AS bit)
    ORDER BY CASE WHEN UPPER(LTRIM(RTRIM(Denominacion))) = N'GENERAL' THEN 0 ELSE 1 END, TipoArticuloId;

    IF @TipoId IS NULL
    BEGIN
        INSERT INTO ALMACEN.TipoArticulo (Denominacion, Descripcion, Estado)
        VALUES (N'GENERAL', N'Remate / catálogo automático', 1);
        SET @TipoId = CONVERT(INT, SCOPE_IDENTITY());
    END;

    SELECT TOP (1) @MarcaId = MarcaId
    FROM ALMACEN.Marca
    WHERE Estado = CAST(1 AS bit)
    ORDER BY CASE WHEN UPPER(LTRIM(RTRIM(Denominacion))) = N'GENERAL' THEN 0 ELSE 1 END, MarcaId;

    IF @MarcaId IS NULL
    BEGIN
        INSERT INTO ALMACEN.Marca (Denominacion, Estado)
        VALUES (N'GENERAL', 1);
        SET @MarcaId = CONVERT(INT, SCOPE_IDENTITY());
    END;

    SELECT TOP (1) @ModeloId = ModeloId
    FROM ALMACEN.Modelo
    WHERE Estado = CAST(1 AS bit)
    ORDER BY CASE WHEN UPPER(LTRIM(RTRIM(Denominacion))) = N'GENERAL' THEN 0 ELSE 1 END, ModeloId;

    IF @ModeloId IS NULL
    BEGIN
        INSERT INTO ALMACEN.Modelo (Denominacion, MarcaId, Estado)
        VALUES (N'GENERAL', @MarcaId, 1);
        SET @ModeloId = CONVERT(INT, SCOPE_IDENTITY());
    END;

    DECLARE @CurPrendaId BIGINT;
    DECLARE @Descripcion NVARCHAR(500);
    DECLARE @Serie NVARCHAR(100);
    DECLARE @Tasacion DECIMAL(18, 2);
    DECLARE @FotoPath NVARCHAR(500);
    DECLARE @ArticuloPedido INT;

    WHILE EXISTS (
        SELECT 1
        FROM CREDITO.Prenda
        WHERE CreditoId = @CreditoId
          AND Estado = N'EN CUSTODIA'
    )
    BEGIN
        SELECT TOP (1)
            @CurPrendaId = PrendaId,
            @Descripcion = Descripcion,
            @Serie = Serie,
            @Tasacion = ValorTasacion,
            @FotoPath = FotoPath
        FROM CREDITO.Prenda
        WHERE CreditoId = @CreditoId
          AND Estado = N'EN CUSTODIA'
        ORDER BY CASE WHEN PrendaId = @PrendaId THEN 0 ELSE 1 END, PrendaId;

        SET @CodArticulo = LEFT('PRM' + CAST(@CurPrendaId AS varchar(16)), 20);

        INSERT INTO ALMACEN.Articulo (
            ModeloId, TipoArticuloId, CodArticulo, Denominacion, Descripcion,
            Imagen, IndPerecible, IndImportado, Estado)
        VALUES (
            @ModeloId, @TipoId, @CodArticulo,
            LEFT(@Descripcion, 200),
            LEFT(CONCAT(N'Remate crédito ', @CreditoId, N' prenda ', @CurPrendaId), 250),
            @FotoPath, 0, 0, 1);

        SET @ArticuloId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT INTO VENTAS.ListaPrecio (ArticuloId, Monto, Descuento, Estado, Puntos, PuntosCanje)
        VALUES (@ArticuloId, @Tasacion, 0, 1, 0, 0);

        INSERT INTO ALMACEN.SerieArticulo (
            NumeroSerie, AlmacenId, ArticuloId, EstadoId, MovimientoDetEntId, MovimientoDetSalId)
        VALUES (
            LEFT(COALESCE(NULLIF(LTRIM(RTRIM(@Serie)), N''), N'PRM' + CAST(@CurPrendaId AS varchar(16))), 20),
            @AlmacenId,
            @ArticuloId,
            2,
            NULL,
            NULL);

        UPDATE CREDITO.Prenda
        SET Estado = N'REMATADO',
            ArticuloRemateId = @ArticuloId,
            UsuarioModId = @UsuarioId,
            FechaMod = @Fecha
        WHERE PrendaId = @CurPrendaId
          AND Estado = N'EN CUSTODIA';

        IF @CurPrendaId = @PrendaId
            SET @ArticuloPedido = @ArticuloId;
    END;

    UPDATE CREDITO.Credito
    SET FechaRemateEjecutado = @Fecha,
        FechaRemate = COALESCE(FechaRemate, DATEADD(DAY, 30, FechaVencimiento)),
        IndIrrecuperable = CAST(1 AS bit),
        UsuarioModId = @UsuarioId,
        FechaMod = @Fecha
    WHERE CreditoId = @CreditoId
      AND OficinaId = @OficinaId;

    SELECT
        @PrendaId AS PrendaId,
        @CreditoId AS CreditoId,
        @ArticuloPedido AS ArticuloId,
        LEFT('PRM' + CAST(@PrendaId AS varchar(16)), 20) AS CodArticulo,
        @TasacionSolicitada AS PrecioVenta,
        @AlmacenId AS AlmacenId;
END
GO
PRINT N'OK seccion 2026-10-27-remate-catalogo-general.sql';
GO

GO
/* ===== INICIO 2026-10-26-retirar-caja-chica-menu.sql ===== */
GO
/*
  Gerencia: caja chica no se usa.
  Quita el ítem del menú y sus permisos de rol.
  No elimina CREDITO.CajaChicaDiario ni movimientos (historial).
*/

SET NOCOUNT ON;

DECLARE @Menus TABLE (MenuId int NOT NULL PRIMARY KEY);

INSERT INTO @Menus (MenuId)
SELECT m.MenuId
FROM MAESTRO.Menu AS m
WHERE m.Denominacion LIKE N'%caja%chica%'
   OR m.Denominacion LIKE N'%cajachica%'
   OR ISNULL(m.Url, N'') LIKE N'%CajaChica%'
   OR ISNULL(m.Url, N'') LIKE N'%caja/chica%'
   OR ISNULL(m.Modulo, N'') LIKE N'%CAJACHICA%';

DELETE rm
FROM MAESTRO.RolMenu AS rm
INNER JOIN @Menus AS x ON x.MenuId = rm.MenuId;

DELETE m
FROM MAESTRO.Menu AS m
INNER JOIN @Menus AS x ON x.MenuId = m.MenuId;

SELECT
    (SELECT COUNT(*) FROM @Menus) AS MenusCajaChicaQuitados,
    (SELECT COUNT(*) FROM MAESTRO.Menu WHERE Denominacion LIKE N'%caja%chica%' OR Denominacion LIKE N'%cajachica%') AS MenusCajaChicaRestantes;
GO
GO
PRINT N'OK seccion 2026-10-26-retirar-caja-chica-menu.sql';
GO

GO
/* ===== INICIO 2026-10-30-retirar-entrada-almacen-menu.sql ===== */
GO
/*
  Gerencia: compra a proveedores no opera en Credix (vive en ventas).
  Quita el ítem de menú Entrada de almacén y sus RolMenu.
  No elimina ALMACEN.Movimiento / SerieArticulo (salida por venta y kardex).
  No toca Transferencia ni Salida.

  Azure CREDITO de prueba primero. Producción solo si gerencia lo pide.
*/

SET NOCOUNT ON;

DECLARE @Menus TABLE (MenuId int NOT NULL PRIMARY KEY);

INSERT INTO @Menus (MenuId)
SELECT m.MenuId
FROM MAESTRO.Menu AS m
WHERE m.IndPadre = CAST(0 AS bit)
  AND (
        ISNULL(m.Url, N'') LIKE N'%Entrada/Index%'
     OR ISNULL(m.Url, N'') LIKE N'%Almacen/Entrada%'
     OR ISNULL(m.Url, N'') LIKE N'%/Entrada%'
     OR (
            m.Denominacion LIKE N'%entrada%'
        AND m.Denominacion LIKE N'%almacen%'
        )
     OR (
            UPPER(LTRIM(RTRIM(m.Denominacion))) = N'ENTRADA'
        AND ISNULL(m.Modulo, N'') LIKE N'%ALMACEN%'
        )
  )
  AND ISNULL(m.Url, N'') NOT LIKE N'%Transferencia%'
  AND ISNULL(m.Url, N'') NOT LIKE N'%Salida%'
  AND ISNULL(m.Url, N'') NOT LIKE N'%Caja%'
  AND ISNULL(m.Url, N'') NOT LIKE N'%Kardex%';

DELETE rm
FROM MAESTRO.RolMenu AS rm
INNER JOIN @Menus AS x ON x.MenuId = rm.MenuId;

DELETE m
FROM MAESTRO.Menu AS m
INNER JOIN @Menus AS x ON x.MenuId = m.MenuId;

SELECT
    (SELECT COUNT(*) FROM @Menus) AS MenusEntradaQuitados,
    (
        SELECT COUNT(*)
        FROM MAESTRO.Menu
        WHERE IndPadre = CAST(0 AS bit)
          AND (
                ISNULL(Url, N'') LIKE N'%Entrada/Index%'
             OR ISNULL(Url, N'') LIKE N'%Almacen/Entrada%'
             OR (Denominacion LIKE N'%entrada%' AND Denominacion LIKE N'%almacen%')
          )
    ) AS MenusEntradaRestantes;
GO
GO
PRINT N'OK seccion 2026-10-30-retirar-entrada-almacen-menu.sql';
GO

GO
/* ===== INICIO 2026-10-30-simulador-domingo-a-lunes.sql ===== */
GO
/*
  Azure CREDITO de prueba (no producción).

  Comparado con los ALTER de prod del 30/10/2026 18:44.

  usp_SimuladorCredito en prod solo salta domingo si FormaPago='D'.
  Emerson: D, S, Q y M — si la cuota cae domingo, pasa al lunes.
  Prendario no: 30 días corridos (EsPrendario=1 o ProductoId=2).

  Único cambio funcional:
    1) 7.º parámetro opcional @SaltarDomingo BIT = 1
       (MVC / EF siguen mandando 6 args → default 1, no se rompe).
    2) El IF de domingo deja de estar atado a diario.
    3) usp_Credito_Ins pasa 0 si es prendario.

  Interés, meses, amortización, tope, GA, central de riesgo: iguales a prod.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROC [CREDITO].[usp_SimuladorCredito]
@Monto DECIMAL(16,2) = 0.0,
@FormaPago CHAR(1)='D',
@NroCuotas INT=26,
@InteresMensual DECIMAL(4,2)= 7,
@FechaPrimerPago DATE = '20140101',
@GastosAdm DECIMAL(16,2) = NULL,
@SaltarDomingo BIT = 1
AS


DECLARE @Sec INT=0,@PlazoMes INT ,@Capital DECIMAL(16,2)=@Monto,@FechaCuota DATE=@FechaPrimerPago, @InteresTotal DECIMAL(16,2)=0
DECLARE @Amortizacion DECIMAL(16,2)=@Monto/@NroCuotas,@Interes DECIMAL(16,2),@GastoAdmCuota DECIMAL(16,2)

-- 1. Se define la columna Saldo en la tabla temporal
DECLARE @tPlanPagos TABLE(Numero INT,Capital DECIMAL(16,2),FechaPago DATE,Amortizacion DECIMAL(16,2),Interes DECIMAL(16,2),GastosAdm DECIMAL(16,2),Cuota DECIMAL(16,2),Saldo DECIMAL(16,2))
DECLARE @Meses INT = 0
-- Variable para realizar la resta sucesiva
DECLARE @SaldoAcumulado DECIMAL(16,2) = @Monto

set datefirst 1

IF @SaltarDomingo IS NULL
    SET @SaltarDomingo = 1

IF @GastosAdm IS NULL
BEGIN
	SET @GastosAdm = 0
	SELECT @GastosAdm = @GastosAdm + CASE WHEN IndPorcentaje=1 THEN @monto*(Valor/100) ELSE valor END
	FROM CREDITO.GastosAdm
	WHERE Estado=1 AND @Monto BETWEEN MontoMinimo AND MontoMaximo
END
SET @GastoAdmCuota=@GastosAdm/@NroCuotas

IF @FormaPago='D' SET @Meses = CEILING(@NroCuotas/CAST(26 AS DECIMAL))
IF @FormaPago='S' SET @Meses = CEILING(@NroCuotas/CAST(4 AS DECIMAL))
IF @FormaPago='Q' SET @Meses = CEILING(@NroCuotas/CAST(2 AS DECIMAL))
IF @FormaPago='M' SET @Meses = @NroCuotas

SET @Interes = @Monto * ((@InteresMensual*@Meses/@NroCuotas)/100)
SET @InteresTotal = @Monto * ((@InteresMensual*@Meses)/100)

WHILE (@Sec<@NroCuotas)
BEGIN
	SET @Sec=@Sec+1

	-- Prod: solo IF @FormaPago='D'. Ahora D/S/Q/M si @SaltarDomingo=1.
	IF @SaltarDomingo = 1 AND datepart(dw, @FechaCuota) = 7
		SET @FechaCuota = DATEADD(DAY,1,@FechaCuota)

	-- 2. Restamos la amortización en cada cuota
	SET @SaldoAcumulado = @SaldoAcumulado - @Amortizacion

	INSERT INTO	@tPlanPagos(Numero,Capital,FechaPago,Amortizacion,Interes,GastosAdm,Saldo)
	VALUES		(@Sec,@Capital,@FechaCuota,@Amortizacion,@Interes,@GastoAdmCuota,@SaldoAcumulado)

	SET @Capital=@Capital-@Amortizacion

	IF @FormaPago='D' SET @FechaCuota = DATEADD(DAY,1,@FechaCuota)
	IF @FormaPago='S' SET @FechaCuota = DATEADD(DAY,7,@FechaCuota)
	IF @FormaPago='Q' SET @FechaCuota = DATEADD(DAY,15,@FechaCuota)
	IF @FormaPago='M' SET @FechaCuota = DATEADD(MONTH,1,@FechaCuota)
END

UPDATE	@tPlanPagos
SET		Amortizacion = Capital,
		GastosAdm = @GastosAdm - (@GastoAdmCuota * (@NroCuotas-1)),
		Interes = @InteresTotal - (@Interes * (@NroCuotas-1)),
		Saldo = 0.00 -- 3. Forzamos que la última cuota cierre con saldo cero
WHERE	Numero=@NroCuotas

UPDATE @tPlanPagos SET Cuota=Amortizacion+Interes+GastosAdm

SELECT * FROM @tPlanPagos
ORDER BY Numero
GO

CREATE OR ALTER PROC [CREDITO].[usp_Credito_Ins]
@SolicitudCreditoId INT,
@ProductoId INT,
@TipoCuota CHAR(1),
@MontoInicial DECIMAL(16,2),
@MontoCredito DECIMAL(16,2),
@MontoGastosAdm DECIMAL(16,2),
@IndGastoAdm CHAR(3),
@FormaPago CHAR(1),
@NroCuotas INT,
@Interes DECIMAL(4,2),
@FechaPrimerPago DATE,
@Observacion VARCHAR(MAX),
@UsuarioId INT,
@IndCentralRiesgo BIT
AS

DECLARE @Mensaje VARCHAR(200)='', @MontoGA DECIMAL(16,2)=0, @Desembolso DECIMAL(16,2) = @MontoCredito,@CentralRiesgo DECIMAL(16,2) = 0

IF EXISTS(SELECT 1 FROM CREDITO.Credito WHERE CreditoId=@SolicitudCreditoId AND Estado<>'CRE')
BEGIN
 SET @Mensaje='ERROR: La Solicitud debe estar en estado CREADA'
 SELECT @Mensaje 'Mensaje'
 RETURN
END

DECLARE @TopeCredito Decimal(15,2) = (SELECT CL.TopeCredito FROM CREDITO.Credito C
										INNER JOIN MAESTRO.Cliente CL ON CL.PersonaId = C.PersonaId
										WHERE C.CreditoId= @SolicitudCreditoId)
IF @TopeCredito IS NOT NULL
BEGIN
	IF @MontoCredito>@TopeCredito
	BEGIN
	 SET @Mensaje='El tope maximo de credito para este cliente es: S/. ' + CAST(@TopeCredito AS VARCHAR(20))  + ' Si requiere mayor credito comuniquese con el administrador.'
	 SELECT @Mensaje 'Mensaje'
	 RETURN
	END
END

IF @IndGastoAdm='CUO'
 SET @MontoGA=@MontoGastosAdm

IF @IndGastoAdm='CAP'
 SET @Desembolso = @MontoCredito - @MontoGastosAdm

DECLARE @SaltarDomingo BIT = 1
IF EXISTS (
    SELECT 1
    FROM CREDITO.Credito
    WHERE CreditoId = @SolicitudCreditoId
      AND (ISNULL(EsPrendario, 0) = 1 OR ProductoId = 2)
)
    SET @SaltarDomingo = 0

/*CREACION PLAN PAGOS*/
DECLARE @tPlanPagos TABLE(Numero INT,Capital DECIMAL(16,2),FechaPago DATE,Amortizacion DECIMAL(16,2),Interes DECIMAL(16,2),GastosAdm DECIMAL(16,2),Cuota DECIMAL(16,2),Saldo DECIMAL(16,2))
INSERT INTO @tPlanPagos
EXEC CREDITO.usp_SimuladorCredito @MontoCredito, @FormaPago, @NroCuotas ,@Interes, @FechaPrimerPago,@MontoGA, @SaltarDomingo

INSERT INTO CREDITO.PlanPago ( CreditoId ,Numero ,Capital ,FechaVencimiento ,Amortizacion ,Interes,GastosAdm ,Cuota,Estado)
SELECT @SolicitudCreditoId 'CreditoId', Numero ,Capital ,FechaPago ,Amortizacion ,Interes,GastosAdm ,Cuota, 'CRE' 'Estado'
FROM @tPlanPagos ORDER BY Numero


IF @IndCentralRiesgo=1
BEGIN
 select @CentralRiesgo = CASE WHEN IndPorcentaje=1 THEN ((Valor * @MontoCredito)/100) ELSE Valor END
 from CREDITO.GastosAdm
 where @MontoCredito BETWEEN MontoMinimo and MontoMaximo
 and Ind=1
END

/*ACTUALIZAR CREDITO*/
UPDATE CREDITO.Credito
SET Estado='PEN' ,
FechaPrimerPago=@FechaPrimerPago,Interes=@Interes,
FormaPago=@FormaPago,NumeroCuotas=@NroCuotas,
MontoInicial=@MontoInicial,
MontoGastosAdm=@MontoGastosAdm - @CentralRiesgo,
CentralRiesgo=@CentralRiesgo,
MontoCredito=@MontoCredito,
MontoDesembolso = @Desembolso,
TipoGastoAdm=@IndGastoAdm,
ProductoId=@ProductoId,
Observacion=ISNULL(@Observacion,''),
TipoCuota=@TipoCuota,
FechaMod=dbo.ufnFecha(),UsuarioModId=@UsuarioId,
FechaVencimiento=(SELECT MAX(FechaPago) FROM @tPlanPagos)
WHERE CreditoId=@SolicitudCreditoId

SELECT @Mensaje 'Mensaje'
GO

/*
  Humo opcional (no inserta crédito). Domingo 1/11/2026:
  D 1 cuota → 2026-11-02
  S/Q/M 1 cuota → 2026-11-02
  M prendario (@SaltarDomingo=0) → 2026-11-01

EXEC CREDITO.usp_SimuladorCredito 1000,'D',1,5,'20261101',0,1;
EXEC CREDITO.usp_SimuladorCredito 1000,'S',1,5,'20261101',0,1;
EXEC CREDITO.usp_SimuladorCredito 1000,'Q',1,5,'20261101',0,1;
EXEC CREDITO.usp_SimuladorCredito 1000,'M',1,5,'20261101',0,1;
EXEC CREDITO.usp_SimuladorCredito 1000,'M',1,5,'20261101',0,0;
*/
GO
PRINT N'OK seccion 2026-10-30-simulador-domingo-a-lunes.sql';
GO

/* ===== VERIFICACION FINAL ===== */
SET NOCOUNT ON;
PRINT N'Base: ' + DB_NAME();
PRINT N'Servidor: ' + @@SERVERNAME;

SELECT
    DB_NAME() AS Base,
    @@SERVERNAME AS Servidor,
    CASE WHEN COL_LENGTH(N'MAESTRO.Cliente', N'Latitud') IS NULL THEN 0 ELSE 1 END AS ClienteLatitud,
    CASE WHEN COL_LENGTH(N'CREDITO.Credito', N'EsPrendario') IS NULL THEN 0 ELSE 1 END AS EsPrendario,
    CASE WHEN COL_LENGTH(N'CREDITO.Credito', N'FechaRemateEjecutado') IS NULL THEN 0 ELSE 1 END AS FechaRemateEjecutado,
    CASE WHEN COL_LENGTH(N'CREDITO.Prenda', N'ArticuloRemateId') IS NULL THEN 0 ELSE 1 END AS ArticuloRemateId,
    CASE WHEN OBJECT_ID(N'CREDITO.Prenda', N'U') IS NULL THEN 0 ELSE 1 END AS TablaPrenda,
    CASE WHEN OBJECT_ID(N'CREDITO.PlaParametro', N'U') IS NULL THEN 0 ELSE 1 END AS TablaPlaParametro,
    CASE WHEN OBJECT_ID(N'MAESTRO.Comunicado', N'U') IS NULL THEN 0 ELSE 1 END AS TablaComunicado,
    CASE WHEN OBJECT_ID(N'CREDITO.usp_DevolverPrenda', N'P') IS NULL THEN 0 ELSE 1 END AS usp_DevolverPrenda,
    CASE WHEN OBJECT_ID(N'CREDITO.usp_RematarPrenda', N'P') IS NULL THEN 0 ELSE 1 END AS usp_RematarPrenda,
    CASE WHEN OBJECT_ID(N'CREDITO.usp_PrendarioRenovar', N'P') IS NULL THEN 0 ELSE 1 END AS usp_PrendarioRenovar,
    CASE WHEN OBJECT_ID(N'CREDITO.usp_PrendarioLiberar', N'P') IS NULL THEN 0 ELSE 1 END AS usp_PrendarioLiberar,
    CASE WHEN OBJECT_ID(N'MAESTRO.usp_ComunicadoListar', N'P') IS NULL THEN 0 ELSE 1 END AS usp_ComunicadoListar,
    CASE WHEN OBJECT_ID(N'CREDITO.usp_MorosidadEmpresa', N'P') IS NULL THEN 0 ELSE 1 END AS usp_MorosidadEmpresa,
    CASE WHEN CHARINDEX(N'@SaltarDomingo', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'CREDITO.usp_SimuladorCredito')), N'')) > 0 THEN 1 ELSE 0 END AS SimuladorSaltarDomingo,
    CASE WHEN CHARINDEX(N'@SaltarDomingo', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'CREDITO.usp_Credito_Ins')), N'')) > 0 THEN 1 ELSE 0 END AS CreditoInsSaltarDomingo
    -- Todo en 1 = paquete aplicado. SimuladorSaltarDomingo y CreditoInsSaltarDomingo deben ser 1.
;

PRINT N'Listo paquete 2026-11-02-prod-deltas en ' + DB_NAME() + N'.';
GO
