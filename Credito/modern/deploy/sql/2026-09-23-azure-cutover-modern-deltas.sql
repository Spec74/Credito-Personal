/*
================================================================================
  [NO USAR para bak de produccion CREDITO20260922 / Master operativo]
================================================================================
  Ese bak YA trae condonacion, cierre gerencial, dashboard SPs, TRF bancos, etc.
  Usa SOLO: deploy/sql/2026-09-23-prod-bak-deltas-minimos.sql
================================================================================
*/
/*
================================================================================
  Azure / restore cutover â€” deltas modernos sobre bak del cliente
================================================================================
  Uso:
    1) Restaurar el BAK/bacpac actual del cliente en Azure (reemplaza la DB vieja).
    2) Ejecutar ESTE script (idempotente) en la base restaurada.
    3) Apuntar Credito.Modern + strangler a esa connection string.

  Incluye:
    A) Geo Cliente/Oficina
    B) Prendario (columnas + Prenda + Ã­ndices + menÃº + Rol ANALISTA)
    C) ClaveUsuario nvarchar(256) para PBKDF2
    D) Mora postergada (CreditoMora.MovimientoCajaId NULLABLE + usp_CreditoMora_*)
    E) CondonaciÃ³n (tabla + usp_SolicitarCondonacion)
    F) BÃ³veda transferencia entre bancos (usp_RegistrarTransferenciaBancos)
    G) Cierre gerencial (tablas + ufn_* + usp_* + trigger)
    H) Ãndices de rendimiento dashboard (recomendados)

  NO recrea el nÃºcleo de negocio: eso viene en el restore.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* -------------------------------------------------------------------------- */
/* A) GEO                                                                     */
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
/* B) PRENDARIO                                                               */
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
IF COL_LENGTH(N'CREDITO.Credito', N'FechaNotifWhatsapp3d') IS NULL
    ALTER TABLE CREDITO.Credito ADD FechaNotifWhatsapp3d datetime NULL;
GO

UPDATE CREDITO.Credito
SET EsPrendario = 1
WHERE ProductoId = 2 AND EsPrendario = 0;
GO

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
/* C) CLAVE PBKDF2                                                            */
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

/* -------------------------------------------------------------------------- */
/* D) MORA POSTERGADA                                                          */
/* -------------------------------------------------------------------------- */
-- Mora postergada (adicion del sistema moderno).
--
-- El flujo moderno permite registrar la mora de una cuota vencida antes de que el cliente
-- la pague. Esa mora queda "pendiente" y se representa con MovimientoCajaId NULL; al cobrarla,
-- usp_CreditoMora_Liquidar crea el movimiento de caja y vincula las filas.
--
-- DIVERGENCIA REGISTRADA: la entrega del cliente de 2026-09-01 define
-- CREDITO.CreditoMora.MovimientoCajaId como NOT NULL, lo que impide representar la mora
-- pendiente. Esta migracion vuelve la columna NULLABLE. Ver docs/migration/BITACORA-DESVIACIONES.md.
--
-- No existe en las entregas de base del cliente: reaplicar tras cada restauracion.

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

    -- Estructura identica a la que devuelve usp_CuotasPendientes.
    DECLARE @tCuotasPendientes TABLE(
        Id INT IDENTITY(1,1), PlanPagoId INT, Glosa VARCHAR(MAX), FechaVencimiento DATE,
        Amortizacion DECIMAL(16,2), Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2),
        Cuota DECIMAL(16,2), DiasAtrazo INT, ImporteMora DECIMAL(16,2),
        Descuento DECIMAL(16,2), Cargo DECIMAL(16,2), PagoLibre DECIMAL(16,2), PagoCuota DECIMAL(16,2)
    );

    -- El calculo de atraso y mora lo hace el core, no se replica aqui.
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

        -- Evita duplicar la mora pendiente del mismo credito.
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

        -- SCOPE_IDENTITY evita capturar el identity de un trigger, a diferencia de @@IDENTITY.
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
GO

/* -------------------------------------------------------------------------- */
/* E) CONDONACIÃ“N                                                              */
/* -------------------------------------------------------------------------- */
-- Flujo de solicitud de condonacion del bak 2026-09-01.
-- Tabla CREDITO.CreditoCondonacion + CREDITO.usp_SolicitarCondonacion.
-- El cuerpo del procedimiento se versiona tal cual (sin reescribir la formula).
-- Idempotente: se reaplica tras cada restauracion de base del cliente.

IF OBJECT_ID(N'CREDITO.CreditoCondonacion', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.CreditoCondonacion (
        Id int IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_CreditoCondonacion PRIMARY KEY,
        CreditoId int NOT NULL,
        CajaDiarioId int NOT NULL,
        Fecha datetime NOT NULL,
        MoraCondonacion decimal(10,2) NOT NULL,
        TotalPago decimal(15,2) NOT NULL,
        IndAprobado bit NOT NULL,
        CONSTRAINT FK_CreditoCondonacion_Credito FOREIGN KEY (CreditoId)
            REFERENCES CREDITO.Credito (CreditoId),
        CONSTRAINT FK_CreditoCondonacion_CajaDiario FOREIGN KEY (CajaDiarioId)
            REFERENCES CREDITO.CajaDiario (CajaDiarioId)
    );
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC [CREDITO].[usp_SolicitarCondonacion]
@CajaDiarioId INT ,
@CreditoId INT ,
@MoraCondonacion DECIMAL(15,2)
 AS

 DECLARE @MontoCredito DECIMAL(15,2) = (SELECT MontoCredito + (MontoCredito * Interes / 100)
 FROM CREDITO.Credito WHERE CreditoId=@CreditoId)

 DECLARE @Pagos DECIMAL(15,2) = (SELECT SUM(ImportePago) FROM CREDITO.MovimientoCaja
 WHERE CreditoId=@CreditoId AND ImportePago>0 AND Operacion='CUO')

 INSERT CREDITO.CreditoCondonacion
 (
     CreditoId,     CajaDiarioId,     Fecha,     MoraCondonacion,     IndAprobado, TotalPago
 )
 VALUES
 (   @CreditoId,  @CajaDiarioId, dbo.ufnFecha(), @MoraCondonacion,       0  , @MontoCredito - @Pagos + @MoraCondonacion)
GO
GO

/* -------------------------------------------------------------------------- */
/* F) BÃ“VEDA TRANSFERENCIA ENTRE BANCOS                                        */
/* -------------------------------------------------------------------------- */
-- Transferencia entre medios de pago de la misma boveda (bak 2026-09-01).
-- Cuerpo de CREDITO.usp_RegistrarTransferenciaBancos versionado tal cual:
-- dos BovedaMov TRF (salida origen / entrada destino). No recalcula importes.
-- Idempotente: se reaplica tras cada restauracion.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_RegistrarTransferenciaBancos]
    @BovedaId INT,
    @TipoPagoOrigenId SMALLINT,
    @TipoPagoDestinoId SMALLINT,
    @Importe DECIMAL(16,2),
    @Glosa VARCHAR(MAX),
    @UsuarioRegId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @TipoPagoOrigenId = @TipoPagoDestinoId
    BEGIN
        RAISERROR('El banco de origen y el de destino no pueden ser iguales.', 16, 1);
        RETURN;
    END

    IF @Importe <= 0
    BEGIN
        RAISERROR('El importe de la transferencia debe ser mayor a cero.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @FechaActual DATETIME = GETDATE();
        DECLARE @CodOperacion CHAR(3) = 'TRF';

        INSERT INTO CREDITO.BovedaMov (
            BovedaId,
            CodOperacion,
            Glosa,
            Importe,
            IndEntrada,
            Estado,
            CajaDiarioId,
            UsuarioRegId,
            FechaReg,
            TipoPagoId
        )
        VALUES (
            @BovedaId,
            @CodOperacion,
            UPPER('Trf. Salida: ' + ISNULL(@Glosa, '')),
            @Importe,
            0,
            1,
            NULL,
            @UsuarioRegId,
            @FechaActual,
            @TipoPagoOrigenId
        );

        INSERT INTO CREDITO.BovedaMov (
            BovedaId,
            CodOperacion,
            Glosa,
            Importe,
            IndEntrada,
            Estado,
            CajaDiarioId,
            UsuarioRegId,
            FechaReg,
            TipoPagoId
        )
        VALUES (
            @BovedaId,
            @CodOperacion,
            UPPER('Trf. Ingreso: ' + ISNULL(@Glosa, '')),
            @Importe,
            1,
            1,
            NULL,
            @UsuarioRegId,
            @FechaActual,
            @TipoPagoDestinoId
        );

        COMMIT TRANSACTION;

        SELECT 1 AS Resultado, 'Transferencia realizada con éxito.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMsg VARCHAR(MAX) = ERROR_MESSAGE();
        SELECT 0 AS Resultado, 'Error: ' + @ErrorMsg AS Mensaje;
    END CATCH
END
GO
GO

/* -------------------------------------------------------------------------- */
/* G) CIERRE GERENCIAL                                                        */
/* -------------------------------------------------------------------------- */
CREATE OR ALTER FUNCTION CREDITO.ufn_FechaGerencial()
        RETURNS DATETIME2(0)
        AS
        BEGIN
            RETURN CONVERT(DATETIME2(0), DATEADD(HOUR, -5, SYSUTCDATETIME()));
        END;
GO


IF OBJECT_ID(N'CREDITO.CierreGerencial', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.CierreGerencial (
        CierreGerencialId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CierreGerencial PRIMARY KEY,
        Periodo date NOT NULL,
        FechaCorte date NOT NULL,
        Estado char(3) NOT NULL,
        Version int NOT NULL CONSTRAINT DF_CierreGerencial_Version DEFAULT (1),
        UsuarioCierreId int NOT NULL,
        FechaCierre datetime2(0) NOT NULL CONSTRAINT DF_CierreGerencial_Fecha DEFAULT (CREDITO.ufn_FechaGerencial()),
        Observacion varchar(500) NULL,
        CONSTRAINT UQ_CierreGerencial_PeriodoVersion UNIQUE (Periodo, Version),
        CONSTRAINT CK_CierreGerencial_Estado CHECK ([Estado]='ANU' OR [Estado]='CER'),
        CONSTRAINT CK_CierreGerencial_FechaCorte CHECK ([FechaCorte]=eomonth([Periodo])),
        CONSTRAINT CK_CierreGerencial_Periodo CHECK (datepart(day,[Periodo])=(1))
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CierreGerencial_PeriodoCerrado' AND object_id = OBJECT_ID(N'CREDITO.CierreGerencial'))
    CREATE UNIQUE INDEX UX_CierreGerencial_PeriodoCerrado
        ON CREDITO.CierreGerencial (Periodo)
        WHERE ([Estado]='CER');
GO

IF OBJECT_ID(N'CREDITO.CierreGerencialDetalle', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.CierreGerencialDetalle (
        CierreGerencialDetalleId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CierreGerencialDetalle PRIMARY KEY,
        CierreGerencialId bigint NOT NULL,
        UsuarioId int NOT NULL,
        NombreUsuario varchar(100) NULL,
        NombreCompleto varchar(250) NULL,
        Asesor varchar(150) NULL,
        Supervisor varchar(150) NULL,
        Mercado varchar(150) NULL,
        Orden smallint NULL,
        TipoCartera varchar(20) NOT NULL CONSTRAINT DF_CierreDetalle_Tipo DEFAULT ('PRODUCTIVA'),
        FuenteInicial varchar(250) NULL,
        CapitalCierre decimal(18,2) NOT NULL CONSTRAINT DF_CierreDetalle_Capital DEFAULT (0),
        TotalClientesActivos int NOT NULL CONSTRAINT DF_CierreDetalle_Clientes DEFAULT (0),
        MoraGeneral decimal(18,2) NOT NULL CONSTRAINT DF_CierreDetalle_Mora DEFAULT (0),
        VencidosCuotasCierre decimal(18,2) NULL,
        ClientesVencidosCierre int NULL,
        DefinicionVencidos varchar(50) NULL,
        ClientesNuevosMes int NULL,
        MontoClientesNuevosMes decimal(18,2) NULL,
        MontoCobradoMes decimal(18,2) NULL,
        DesembolsosMes decimal(18,2) NULL,
        NroOperacionesMes int NULL,
        CONSTRAINT UQ_CierreDetalle_CierreUsuario UNIQUE (CierreGerencialId, UsuarioId),
        CONSTRAINT FK_CierreDetalle_Cierre FOREIGN KEY (CierreGerencialId)
            REFERENCES CREDITO.CierreGerencial (CierreGerencialId)
    );
END
GO

IF OBJECT_ID(N'CREDITO.MetaGerencialAnalista', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.MetaGerencialAnalista (
        MetaGerencialAnalistaId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_MetaGerencialAnalista PRIMARY KEY,
        Periodo date NOT NULL,
        UsuarioId int NOT NULL,
        Asesor varchar(150) NULL,
        Supervisor varchar(150) NULL,
        Mercado varchar(150) NULL,
        Orden smallint NULL,
        TipoCartera varchar(20) NOT NULL CONSTRAINT DF_MetaGerencial_Tipo DEFAULT ('PRODUCTIVA'),
        FuenteInicial varchar(250) NULL,
        MetaCapitalCierre decimal(18,2) NULL,
        MetaClientesActivosCierre int NULL,
        MetaVencidosMaximoCierre decimal(18,2) NULL,
        MetaRecuperacionVencidosMes decimal(18,2) NULL,
        Activo bit NOT NULL CONSTRAINT DF_MetaGerencial_Activo DEFAULT (1),
        UsuarioRegistroId int NULL,
        FechaRegistro datetime2(0) NOT NULL CONSTRAINT DF_MetaGerencial_Fecha DEFAULT (CREDITO.ufn_FechaGerencial()),
        UsuarioModificacionId int NULL,
        FechaModificacion datetime2(0) NULL,
        CONSTRAINT UQ_MetaGerencial_PeriodoUsuario UNIQUE (Periodo, UsuarioId),
        CONSTRAINT CK_MetaGerencial_Periodo CHECK (datepart(day,[Periodo])=(1)),
        CONSTRAINT CK_MetaGerencial_Tipo CHECK ([TipoCartera]='ESPECIAL' OR [TipoCartera]='PRODUCTIVA'),
        CONSTRAINT CK_MetaGerencial_Valores CHECK (
            ([MetaCapitalCierre] IS NULL OR [MetaCapitalCierre]>=(0))
            AND ([MetaClientesActivosCierre] IS NULL OR [MetaClientesActivosCierre]>=(0))
            AND ([MetaVencidosMaximoCierre] IS NULL OR [MetaVencidosMaximoCierre]>=(0))
            AND ([MetaRecuperacionVencidosMes] IS NULL OR [MetaRecuperacionVencidosMes]>=(0))
        )
    );
END
GO

IF OBJECT_ID(N'CREDITO.VencidoGerencialAperturaDetalle', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.VencidoGerencialAperturaDetalle (
        VencidoGerencialAperturaDetalleId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_VencidoGerencialAperturaDetalle PRIMARY KEY,
        Periodo date NOT NULL,
        UsuarioId int NOT NULL,
        CreditoId int NOT NULL,
        PlanPagoId int NOT NULL,
        FechaVencimiento date NOT NULL,
        SaldoVencidoApertura decimal(18,2) NOT NULL,
        CierreOrigenId bigint NOT NULL,
        FechaRegistro datetime2(0) NOT NULL,
        CONSTRAINT UQ_VencidoGerencialApertura_PeriodoPlan UNIQUE (Periodo, PlanPagoId),
        CONSTRAINT CK_VencidoGerencialApertura_Periodo CHECK (datepart(day,[Periodo])=(1)),
        CONSTRAINT CK_VencidoGerencialApertura_Saldo CHECK ([SaldoVencidoApertura]>(0)),
        CONSTRAINT FK_VencidoGerencialApertura_Cierre FOREIGN KEY (CierreOrigenId)
            REFERENCES CREDITO.CierreGerencial (CierreGerencialId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_VencidoGerencialApertura_Usuario' AND object_id = OBJECT_ID(N'CREDITO.VencidoGerencialAperturaDetalle'))
    CREATE INDEX IX_VencidoGerencialApertura_Usuario
        ON CREDITO.VencidoGerencialAperturaDetalle (Periodo, UsuarioId)
        INCLUDE (CreditoId, PlanPagoId, SaldoVencidoApertura);
GO

/* Saldo FIFO de todas las cuotas de los creditos activos. */
CREATE OR ALTER FUNCTION CREDITO.ufn_SaldosCuotasGerenciales
(
    @FechaCorte DATE,
    @OficinaId INT
)
RETURNS TABLE
AS
RETURN
(
    WITH Carteras AS
    (
        SELECT M.UsuarioId
        FROM CREDITO.MetaGerencialAnalista M
        WHERE M.Periodo = DATEFROMPARTS(YEAR(@FechaCorte), MONTH(@FechaCorte), 1)
          AND M.Activo = 1
    ),
    CreditosActuales AS
    (
        SELECT C.CreditoId, C.UsuarioRegId AS UsuarioId, C.PersonaId
        FROM CREDITO.Credito C
        INNER JOIN Carteras A ON A.UsuarioId = C.UsuarioRegId
        WHERE C.OficinaId = @OficinaId AND C.Estado = 'DES'
    ),
    Pagos AS
    (
        SELECT M.CreditoId, SUM(M.ImportePago) AS TotalPagado
        FROM CREDITO.MovimientoCaja M
        INNER JOIN CreditosActuales C ON C.CreditoId = M.CreditoId
        WHERE M.Operacion = 'CUO' AND M.Estado = 1 AND M.IndEntrada = 1
          AND M.ImportePago > 0 AND M.FechaReg < DATEADD(DAY, 1, @FechaCorte)
        GROUP BY M.CreditoId
    ),
    Cuotas AS
    (
        SELECT C.UsuarioId, C.PersonaId, C.CreditoId, P.PlanPagoId,
               P.FechaVencimiento,
               CAST(P.Cuota + P.Cargo AS DECIMAL(18,2)) AS ImporteCuota,
               CAST(ISNULL(G.TotalPagado,0) AS DECIMAL(18,2)) AS TotalPagado,
               CAST(ISNULL(SUM(P.Cuota + P.Cargo) OVER
                    (PARTITION BY P.CreditoId ORDER BY P.FechaVencimiento, P.PlanPagoId
                     ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING),0)
                    AS DECIMAL(18,2)) AS AcumuladoAnterior,
               CAST(SUM(P.Cuota + P.Cargo) OVER
                    (PARTITION BY P.CreditoId ORDER BY P.FechaVencimiento, P.PlanPagoId
                     ROWS UNBOUNDED PRECEDING) AS DECIMAL(18,2)) AS AcumuladoIncluido
        FROM CreditosActuales C
        INNER JOIN CREDITO.PlanPago P ON P.CreditoId = C.CreditoId
        LEFT JOIN Pagos G ON G.CreditoId = C.CreditoId
    )
    SELECT Q.UsuarioId, Q.PersonaId, Q.CreditoId, Q.PlanPagoId,
           Q.FechaVencimiento, Q.ImporteCuota,
           CAST(CASE WHEN Q.TotalPagado >= Q.AcumuladoIncluido THEN 0
                     WHEN Q.TotalPagado <= Q.AcumuladoAnterior THEN Q.ImporteCuota
                     ELSE Q.AcumuladoIncluido - Q.TotalPagado END
                AS DECIMAL(18,2)) AS SaldoCuota
    FROM Cuotas Q
);
GO


CREATE OR ALTER FUNCTION CREDITO.ufn_ClientesNuevosGerenciales
(
    @Periodo DATE,
    @OficinaId INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT C.UsuarioRegId AS UsuarioId,
           COUNT(DISTINCT C.PersonaId) AS ClientesNuevosMes
    FROM CREDITO.Credito C
    INNER JOIN MAESTRO.Cliente CL ON CL.PersonaId = C.PersonaId
    WHERE CL.FechaRegistro >= DATEFROMPARTS(YEAR(@Periodo), MONTH(@Periodo), 1)
      AND CL.FechaRegistro < DATEADD(MONTH, 1,
            DATEFROMPARTS(YEAR(@Periodo), MONTH(@Periodo), 1))
      AND C.OficinaId = @OficinaId AND C.UsuarioRegId IS NOT NULL
    GROUP BY C.UsuarioRegId
);
GO


/* Hechos mensuales. No usa el estado de la cartera para calcular saldos. */
CREATE OR ALTER FUNCTION CREDITO.ufn_ActividadGerencialMensual
(
    @Periodo DATE,
    @FechaCorte DATE,
    @OficinaId INT
)
RETURNS TABLE
AS
RETURN
(
    WITH Parametros AS
    (
        SELECT DATEFROMPARTS(YEAR(@Periodo), MONTH(@Periodo), 1) AS FechaInicio,
               DATEADD(DAY, 1, @FechaCorte) AS FechaFinExclusiva
    ),
    Carteras AS
    (
        SELECT M.UsuarioId
        FROM CREDITO.MetaGerencialAnalista M
        WHERE M.Periodo = DATEFROMPARTS(YEAR(@Periodo), MONTH(@Periodo), 1)
          AND M.Activo = 1
    ),
    ClientesNuevos AS
    (
        SELECT C.UsuarioRegId AS UsuarioId,
               COUNT(DISTINCT C.PersonaId) AS ClientesNuevosMes,
               CAST(ISNULL(SUM(C.MontoDesembolso), 0) AS DECIMAL(18,2))
                   AS MontoClientesNuevosMes
        FROM CREDITO.Credito C
        INNER JOIN MAESTRO.Cliente CL ON CL.PersonaId = C.PersonaId
        CROSS JOIN Parametros P
        WHERE CL.FechaRegistro >= P.FechaInicio
          AND CL.FechaRegistro < DATEADD(MONTH, 1, P.FechaInicio)
          AND C.FechaDesembolso >= P.FechaInicio
          AND C.FechaDesembolso < P.FechaFinExclusiva
          AND C.OficinaId = @OficinaId
          AND C.UsuarioRegId IS NOT NULL
          AND C.MontoDesembolso > 0
          AND C.Estado IN ('DES', 'PAG')
        GROUP BY C.UsuarioRegId
    ),
    Cobranza AS
    (
        SELECT C.UsuarioRegId AS UsuarioId,
               CAST(ISNULL(SUM(M.ImportePago), 0) AS DECIMAL(18,2)) AS MontoCobradoMes
        FROM CREDITO.MovimientoCaja M
        INNER JOIN CREDITO.Credito C ON C.CreditoId = M.CreditoId
        CROSS JOIN Parametros P
        WHERE M.FechaReg >= P.FechaInicio
          AND M.FechaReg < P.FechaFinExclusiva
          AND M.Operacion = 'CUO'
          AND M.Estado = 1
          AND M.IndEntrada = 1
          AND M.ImportePago > 0
          AND C.OficinaId = @OficinaId
          AND C.UsuarioRegId IS NOT NULL
        GROUP BY C.UsuarioRegId
    ),
    Desembolsos AS
    (
        SELECT C.UsuarioRegId AS UsuarioId,
               CAST(ISNULL(SUM(C.MontoDesembolso), 0) AS DECIMAL(18,2)) AS DesembolsosMes,
               COUNT(*) AS NroOperacionesMes
        FROM CREDITO.Credito C
        CROSS JOIN Parametros P
        WHERE C.FechaDesembolso >= P.FechaInicio
          AND C.FechaDesembolso < P.FechaFinExclusiva
          AND C.OficinaId = @OficinaId
          AND C.UsuarioRegId IS NOT NULL
          AND C.MontoDesembolso > 0
          AND C.Estado IN ('DES', 'PAG')
        GROUP BY C.UsuarioRegId
    )
    SELECT K.UsuarioId,
           ISNULL(N.ClientesNuevosMes, 0) AS ClientesNuevosMes,
           CAST(ISNULL(N.MontoClientesNuevosMes, 0) AS DECIMAL(18,2))
               AS MontoClientesNuevosMes,
           CAST(ISNULL(B.MontoCobradoMes, 0) AS DECIMAL(18,2)) AS MontoCobradoMes,
           CAST(ISNULL(D.DesembolsosMes, 0) AS DECIMAL(18,2)) AS DesembolsosMes,
           ISNULL(D.NroOperacionesMes, 0) AS NroOperacionesMes
    FROM Carteras K
    LEFT JOIN ClientesNuevos N ON N.UsuarioId = K.UsuarioId
    LEFT JOIN Cobranza B ON B.UsuarioId = K.UsuarioId
    LEFT JOIN Desembolsos D ON D.UsuarioId = K.UsuarioId
);
GO


CREATE OR ALTER FUNCTION CREDITO.ufn_MetricasGerencialesActuales
(
    @FechaCorte DATE,
    @OficinaId INT
)
RETURNS TABLE
AS
RETURN
(
    WITH Carteras AS
    (
        SELECT M.UsuarioId
        FROM CREDITO.MetaGerencialAnalista M
        WHERE M.Periodo = DATEFROMPARTS(YEAR(@FechaCorte), MONTH(@FechaCorte), 1)
          AND M.Activo = 1
    ),
    Saldos AS
    (
        SELECT * FROM CREDITO.ufn_SaldosCuotasGerenciales(@FechaCorte, @OficinaId)
    )
    SELECT C.UsuarioId,
           CAST(ISNULL(SUM(S.SaldoCuota),0) AS DECIMAL(18,2)) AS CapitalActual,
           COUNT(DISTINCT CASE WHEN S.SaldoCuota > 0 THEN S.PersonaId END) AS ClientesActivosActual,
           CAST(ISNULL(SUM(CASE WHEN S.FechaVencimiento < @FechaCorte
                               THEN S.SaldoCuota ELSE 0 END),0) AS DECIMAL(18,2)) AS VencidosActual,
           COUNT(DISTINCT CASE WHEN S.FechaVencimiento < @FechaCorte AND S.SaldoCuota > 0
                               THEN S.PersonaId END) AS ClientesVencidosActual
    FROM Carteras C
    LEFT JOIN Saldos S ON S.UsuarioId = C.UsuarioId
    GROUP BY C.UsuarioId
);
GO


CREATE OR ALTER FUNCTION CREDITO.ufn_RecuperacionVencidosGerencial
(
    @Periodo DATE,
    @FechaCorte DATE
)
RETURNS TABLE
AS
RETURN
(
    WITH Apertura AS
    (
        SELECT A.UsuarioId, A.CreditoId, A.PlanPagoId, A.SaldoVencidoApertura
        FROM CREDITO.VencidoGerencialAperturaDetalle A
        WHERE A.Periodo = DATEFROMPARTS(YEAR(@Periodo), MONTH(@Periodo), 1)
    ),
    CreditosApertura AS
    (
        SELECT DISTINCT A.CreditoId FROM Apertura A
    ),
    Pagos AS
    (
        SELECT M.CreditoId, SUM(M.ImportePago) AS TotalPagado
        FROM CREDITO.MovimientoCaja M
        INNER JOIN CreditosApertura C ON C.CreditoId = M.CreditoId
        WHERE M.Operacion = 'CUO' AND M.Estado = 1 AND M.IndEntrada = 1
          AND M.ImportePago > 0 AND M.FechaReg < DATEADD(DAY,1,@FechaCorte)
        GROUP BY M.CreditoId
    ),
    Cuotas AS
    (
        SELECT P.CreditoId, P.PlanPagoId,
               CAST(P.Cuota + P.Cargo AS DECIMAL(18,2)) AS ImporteCuota,
               CAST(ISNULL(G.TotalPagado,0) AS DECIMAL(18,2)) AS TotalPagado,
               CAST(ISNULL(SUM(P.Cuota + P.Cargo) OVER
                    (PARTITION BY P.CreditoId ORDER BY P.FechaVencimiento,P.PlanPagoId
                     ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING),0)
                    AS DECIMAL(18,2)) AS AcumuladoAnterior,
               CAST(SUM(P.Cuota + P.Cargo) OVER
                    (PARTITION BY P.CreditoId ORDER BY P.FechaVencimiento,P.PlanPagoId
                     ROWS UNBOUNDED PRECEDING) AS DECIMAL(18,2)) AS AcumuladoIncluido
        FROM CREDITO.PlanPago P
        INNER JOIN CreditosApertura C ON C.CreditoId = P.CreditoId
        LEFT JOIN Pagos G ON G.CreditoId = P.CreditoId
    ),
    Saldos AS
    (
        SELECT Q.PlanPagoId,
               CAST(CASE WHEN Q.TotalPagado >= Q.AcumuladoIncluido THEN 0
                         WHEN Q.TotalPagado <= Q.AcumuladoAnterior THEN Q.ImporteCuota
                         ELSE Q.AcumuladoIncluido - Q.TotalPagado END
                    AS DECIMAL(18,2)) AS SaldoActual
        FROM Cuotas Q
    ),
    Comparacion AS
    (
        SELECT A.UsuarioId, A.SaldoVencidoApertura,
               CAST(CASE WHEN ISNULL(S.SaldoActual,0) <= 0 THEN 0
                         WHEN S.SaldoActual >= A.SaldoVencidoApertura
                              THEN A.SaldoVencidoApertura
                         ELSE S.SaldoActual END AS DECIMAL(18,2)) AS SaldoPendienteBase
        FROM Apertura A
        LEFT JOIN Saldos S ON S.PlanPagoId = A.PlanPagoId
    )
    SELECT C.UsuarioId,
           CAST(SUM(C.SaldoVencidoApertura) AS DECIMAL(18,2)) AS VencidosApertura,
           CAST(SUM(C.SaldoPendienteBase) AS DECIMAL(18,2)) AS VencidosAperturaPendiente,
           CAST(SUM(C.SaldoVencidoApertura-C.SaldoPendienteBase) AS DECIMAL(18,2))
                AS RecuperacionVencidosActual
    FROM Comparacion C GROUP BY C.UsuarioId
);
GO


CREATE OR ALTER PROCEDURE CREDITO.usp_GenerarCierreGerencialMensual
    @Periodo DATE,
    @OficinaId INT,
    @UsuarioCierreId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @Periodo IS NULL OR @OficinaId IS NULL OR @UsuarioCierreId IS NULL
        THROW 53040, 'Periodo, oficina y usuario de cierre son obligatorios.',1;

    SET @Periodo=DATEFROMPARTS(YEAR(@Periodo),MONTH(@Periodo),1);
    DECLARE @Ahora DATETIME2(0)=CREDITO.ufn_FechaGerencial();
    DECLARE @FechaHoy DATE=CAST(@Ahora AS DATE);
    DECLARE @PeriodoActual DATE=DATEFROMPARTS(YEAR(@Ahora),MONTH(@Ahora),1);
    DECLARE @PeriodoAnterior DATE=DATEADD(MONTH,-1,@PeriodoActual);
    DECLARE @FechaCorte DATE=EOMONTH(@Periodo);
    DECLARE @PeriodoSiguiente DATE=DATEADD(MONTH,1,@Periodo);
    DECLARE @CierreId BIGINT,@Bloqueo INT;

    IF @Periodo<>@PeriodoActual AND @Periodo<>@PeriodoAnterior
        THROW 53041, 'Solo puede cerrarse el periodo actual o el inmediatamente anterior.',1;
    IF @Periodo=@PeriodoActual AND @FechaHoy<>@FechaCorte
        THROW 53042, 'El periodo actual solo puede cerrarse en su ultimo dia.',1;
    IF @Periodo=@PeriodoAnterior AND DAY(@FechaHoy)>2
        THROW 53043, 'La contingencia para el mes anterior termina el dia 2.',1;
    IF (SELECT COUNT(*) FROM CREDITO.MetaGerencialAnalista
        WHERE Periodo=@Periodo AND Activo=1)<>19
        THROW 53044, 'El periodo no contiene las 19 carteras esperadas.',1;
    IF (SELECT COUNT(*) FROM CREDITO.MetaGerencialAnalista
        WHERE Periodo=@Periodo AND Activo=1 AND TipoCartera='PRODUCTIVA')<>16
        THROW 53045, 'El periodo no contiene las 16 carteras productivas.',1;
    IF (SELECT COUNT(*) FROM CREDITO.MetaGerencialAnalista
        WHERE Periodo=@Periodo AND Activo=1 AND TipoCartera='ESPECIAL')<>3
        THROW 53046, 'El periodo no contiene las 3 carteras especiales.',1;
    IF EXISTS
    (
        SELECT 1 FROM CREDITO.MetaGerencialAnalista M
        WHERE M.Periodo=@Periodo AND M.Activo=1
          AND (M.MetaVencidosMaximoCierre IS NULL OR M.MetaRecuperacionVencidosMes IS NULL
               OR (M.TipoCartera='PRODUCTIVA' AND
                   (M.MetaCapitalCierre IS NULL OR M.MetaClientesActivosCierre IS NULL)))
    )
        THROW 53047, 'No puede cerrarse el periodo porque faltan metas.',1;

    BEGIN TRY
        BEGIN TRANSACTION;
        EXEC @Bloqueo=sys.sp_getapplock
            @Resource=N'CREDITO.CIERRE_GERENCIAL_MENSUAL',@LockMode=N'Exclusive',
            @LockOwner=N'Transaction',@LockTimeout=10000;
        IF @Bloqueo<0 THROW 53048, 'No se pudo obtener el bloqueo exclusivo del cierre.',1;

        SELECT @CierreId=CierreGerencialId
        FROM CREDITO.CierreGerencial WITH(UPDLOCK,HOLDLOCK)
        WHERE Periodo=@Periodo AND Estado='CER';
        IF @CierreId IS NOT NULL
        BEGIN
            COMMIT TRANSACTION;
            SELECT @CierreId AS CierreGerencialId,@Periodo AS Periodo,CAST(0 AS BIT) AS Creado,
                   'EL CIERRE YA EXISTIA; NO FUE MODIFICADO' AS Resultado;
            RETURN;
        END;

        INSERT INTO CREDITO.CierreGerencial
            (Periodo,FechaCorte,Estado,Version,UsuarioCierreId,FechaCierre,Observacion)
        VALUES(@Periodo,@FechaCorte,'CER',1,@UsuarioCierreId,@Ahora,
               'CIERRE OFICIAL: CUOTAS FIFO, PAGOS VALIDOS Y CLIENTES NUEVOS');
        SET @CierreId=SCOPE_IDENTITY();

        INSERT INTO CREDITO.CierreGerencialDetalle
        (
            CierreGerencialId,UsuarioId,NombreUsuario,NombreCompleto,Asesor,Supervisor,
            Mercado,Orden,TipoCartera,FuenteInicial,CapitalCierre,TotalClientesActivos,
            MoraGeneral,VencidosCuotasCierre,ClientesVencidosCierre,DefinicionVencidos,
            ClientesNuevosMes
        )
        SELECT @CierreId,M.UsuarioId,CAST(U.NombreUsuario AS VARCHAR(100)),
               CAST(P.NombreCompleto AS VARCHAR(250)),M.Asesor,M.Supervisor,M.Mercado,
               M.Orden,M.TipoCartera,'CREDITO.ufn_MetricasGerencialesActuales',
               A.CapitalActual,A.ClientesActivosActual,A.VencidosActual,A.VencidosActual,
               A.ClientesVencidosActual,'CUOTAS_FIFO_PAGOS_VALIDOS',
               ISNULL(N.ClientesNuevosMes,0)
        FROM CREDITO.MetaGerencialAnalista M
        INNER JOIN MAESTRO.Usuario U ON U.UsuarioId=M.UsuarioId
        LEFT JOIN MAESTRO.Persona P ON P.PersonaId=U.PersonaId
        INNER JOIN CREDITO.ufn_MetricasGerencialesActuales(@FechaCorte,@OficinaId) A
            ON A.UsuarioId=M.UsuarioId
        LEFT JOIN CREDITO.ufn_ClientesNuevosGerenciales(@Periodo,@OficinaId) N
            ON N.UsuarioId=M.UsuarioId
        WHERE M.Periodo=@Periodo AND M.Activo=1;
        IF @@ROWCOUNT<>19 THROW 53049, 'El detalle calculado no contiene las 19 carteras.',1;

        INSERT INTO CREDITO.VencidoGerencialAperturaDetalle
            (Periodo,UsuarioId,CreditoId,PlanPagoId,FechaVencimiento,
             SaldoVencidoApertura,CierreOrigenId,FechaRegistro)
        SELECT @PeriodoSiguiente,Q.UsuarioId,Q.CreditoId,Q.PlanPagoId,Q.FechaVencimiento,
               Q.SaldoCuota,@CierreId,@Ahora
        FROM CREDITO.ufn_SaldosCuotasGerenciales(@FechaCorte,@OficinaId) Q
        WHERE Q.FechaVencimiento<@FechaCorte AND Q.SaldoCuota>0;

        MERGE CREDITO.MetaGerencialAnalista WITH(HOLDLOCK) AS D
        USING
        (
            SELECT UsuarioId,Asesor,Supervisor,Mercado,Orden,TipoCartera
            FROM CREDITO.MetaGerencialAnalista WHERE Periodo=@Periodo AND Activo=1
        ) O
        ON D.Periodo=@PeriodoSiguiente AND D.UsuarioId=O.UsuarioId
        WHEN MATCHED THEN UPDATE SET
            Asesor=O.Asesor,Supervisor=O.Supervisor,Mercado=O.Mercado,Orden=O.Orden,
            TipoCartera=O.TipoCartera,FuenteInicial=CONCAT('CIERRE GERENCIAL ',@CierreId),
            Activo=1,UsuarioModificacionId=@UsuarioCierreId,FechaModificacion=@Ahora
        WHEN NOT MATCHED THEN INSERT
            (Periodo,UsuarioId,Asesor,Supervisor,Mercado,Orden,TipoCartera,FuenteInicial,
             MetaCapitalCierre,MetaClientesActivosCierre,MetaVencidosMaximoCierre,
             MetaRecuperacionVencidosMes,Activo,UsuarioRegistroId,FechaRegistro)
        VALUES(@PeriodoSiguiente,O.UsuarioId,O.Asesor,O.Supervisor,O.Mercado,O.Orden,
               O.TipoCartera,CONCAT('CIERRE GERENCIAL ',@CierreId),NULL,NULL,NULL,NULL,1,
               @UsuarioCierreId,@Ahora);

        COMMIT TRANSACTION;
        SELECT @CierreId AS CierreGerencialId,@Periodo AS Periodo,@FechaCorte AS FechaCorte,
               @PeriodoSiguiente AS PeriodoSiguiente,CAST(1 AS BIT) AS Creado,
               'CIERRE GENERADO CORRECTAMENTE' AS Resultado;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO


/* Llamado seguro desde el cierre de boveda. */
CREATE OR ALTER PROCEDURE CREDITO.usp_IntentarGenerarCierreGerencialMensual
    @OficinaId INT,
    @UsuarioCierreId INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ahora DATETIME2(0)=CREDITO.ufn_FechaGerencial();
    DECLARE @Hoy DATE=CAST(@Ahora AS DATE);
    DECLARE @Periodo DATE=DATEFROMPARTS(YEAR(@Ahora),MONTH(@Ahora),1);
    DECLARE @PeriodoAnterior DATE=DATEADD(MONTH,-1,@Periodo);

    IF @Hoy=EOMONTH(@Hoy)
        EXEC CREDITO.usp_GenerarCierreGerencialMensual
            @Periodo=@Periodo,@OficinaId=@OficinaId,@UsuarioCierreId=@UsuarioCierreId;
    ELSE IF DAY(@Hoy)<=2 AND NOT EXISTS
        (SELECT 1 FROM CREDITO.CierreGerencial
         WHERE Periodo=@PeriodoAnterior AND Estado='CER')
        EXEC CREDITO.usp_GenerarCierreGerencialMensual
            @Periodo=@PeriodoAnterior,@OficinaId=@OficinaId,
            @UsuarioCierreId=@UsuarioCierreId;
END;
GO


CREATE OR ALTER PROCEDURE CREDITO.usp_GuardarMetaGerencialDefinitiva
    @Periodo DATE,
    @UsuarioId INT,
    @MetaCapitalCierre DECIMAL(18,2),
    @MetaClientesActivosCierre INT,
    @MetaVencidosMaximoCierre DECIMAL(18,2),
    @MetaRecuperacionVencidosMes DECIMAL(18,2),
    @UsuarioRegistroId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @UsuarioRegistroId<>10 THROW 53020, 'Solo BRIGIDA esta autorizada para registrar metas.',1;
    IF @Periodo IS NULL OR @UsuarioId IS NULL THROW 53021, 'Periodo y analista son obligatorios.',1;
    SET @Periodo=DATEFROMPARTS(YEAR(@Periodo),MONTH(@Periodo),1);

    DECLARE @Ahora DATETIME2(0)=CREDITO.ufn_FechaGerencial();
    DECLARE @PeriodoActual DATE=DATEFROMPARTS(YEAR(@Ahora),MONTH(@Ahora),1);
    DECLARE @FechaLimite DATETIME2(0)=DATEADD(SECOND,-1,DATEADD(DAY,5,CAST(@Periodo AS DATETIME2(0))));
    DECLARE @Tipo VARCHAR(20);

    IF @Periodo<>@PeriodoActual OR @Ahora<@Periodo OR @Ahora>@FechaLimite
        THROW 53022, 'Las metas solo pueden modificarse durante los dias 1 al 5 del periodo actual.',1;
    IF EXISTS(SELECT 1 FROM CREDITO.CierreGerencial WHERE Periodo=@Periodo AND Estado='CER')
        THROW 53023, 'No se pueden modificar metas de un periodo cerrado.',1;

    SELECT @Tipo=TipoCartera FROM CREDITO.MetaGerencialAnalista
    WHERE Periodo=@Periodo AND UsuarioId=@UsuarioId AND Activo=1;
    IF @Tipo IS NULL THROW 53024, 'El analista no pertenece a la configuracion activa del periodo.',1;

    IF @MetaVencidosMaximoCierre IS NULL OR @MetaRecuperacionVencidosMes IS NULL
        THROW 53025, 'Las dos metas de vencidos son obligatorias.',1;
    IF @Tipo='PRODUCTIVA' AND (@MetaCapitalCierre IS NULL OR @MetaClientesActivosCierre IS NULL)
        THROW 53026, 'Capital y clientes son obligatorios para una cartera productiva.',1;
    IF ISNULL(@MetaCapitalCierre,0)<0 OR ISNULL(@MetaClientesActivosCierre,0)<0
       OR @MetaVencidosMaximoCierre<0 OR @MetaRecuperacionVencidosMes<0
        THROW 53027, 'Las metas no pueden ser negativas.',1;

    UPDATE CREDITO.MetaGerencialAnalista
       SET MetaCapitalCierre=CASE WHEN @Tipo='ESPECIAL' THEN NULL ELSE @MetaCapitalCierre END,
           MetaClientesActivosCierre=CASE WHEN @Tipo='ESPECIAL' THEN NULL ELSE @MetaClientesActivosCierre END,
           MetaVencidosMaximoCierre=@MetaVencidosMaximoCierre,
           MetaRecuperacionVencidosMes=@MetaRecuperacionVencidosMes,
           UsuarioModificacionId=@UsuarioRegistroId, FechaModificacion=@Ahora
     WHERE Periodo=@Periodo AND UsuarioId=@UsuarioId AND Activo=1;

    SELECT MetaGerencialAnalistaId,Periodo,UsuarioId,MetaCapitalCierre,
           MetaClientesActivosCierre,MetaVencidosMaximoCierre,
           MetaRecuperacionVencidosMes,Activo,UsuarioRegistroId,FechaRegistro,
           UsuarioModificacionId,FechaModificacion
    FROM CREDITO.MetaGerencialAnalista
    WHERE Periodo=@Periodo AND UsuarioId=@UsuarioId;
END;
GO


/* El formulario de BRIGIDA: base heredada y ventana de edicion dias 1 al 5. */
CREATE OR ALTER PROCEDURE CREDITO.usp_ListarMetasGerencialesDefinitivas
    @Periodo DATE
AS
BEGIN
    SET NOCOUNT ON;
    IF @Periodo IS NULL THROW 53010, 'El periodo es obligatorio.', 1;

    SET @Periodo = DATEFROMPARTS(YEAR(@Periodo),MONTH(@Periodo),1);
    DECLARE @Ahora DATETIME2(0) = CREDITO.ufn_FechaGerencial();
    DECLARE @PeriodoActual DATE = DATEFROMPARTS(YEAR(@Ahora),MONTH(@Ahora),1);
    DECLARE @FechaLimite DATETIME2(0) = DATEADD(SECOND,-1,DATEADD(DAY,5,CAST(@Periodo AS DATETIME2(0))));
    DECLARE @PeriodoCerrado BIT = CASE WHEN EXISTS
        (SELECT 1 FROM CREDITO.CierreGerencial WHERE Periodo=@Periodo AND Estado='CER')
        THEN 1 ELSE 0 END;

    ;WITH Anterior AS
    (
        SELECT D.*
        FROM CREDITO.CierreGerencial C
        INNER JOIN CREDITO.CierreGerencialDetalle D
            ON D.CierreGerencialId=C.CierreGerencialId
        WHERE C.Periodo=DATEADD(MONTH,-1,@Periodo) AND C.Estado='CER'
    ),
    Cartera AS
    (
        SELECT M.*
        FROM CREDITO.MetaGerencialAnalista M
        WHERE M.Periodo=@Periodo AND M.Activo=1
    )
    SELECT M.UsuarioId, CAST(U.NombreUsuario AS VARCHAR(100)) AS NombreUsuario,
           CAST(P.NombreCompleto AS VARCHAR(250)) AS NombreCompleto, @Periodo AS Periodo,
           CAST(ISNULL(A.CapitalCierre,0) AS DECIMAL(18,2)) AS CapitalBase,
           ISNULL(A.TotalClientesActivos,0) AS ClientesActivosBase,
           CAST(A.VencidosCuotasCierre AS DECIMAL(18,2)) AS VencidosBaseComparable,
           CAST(ISNULL(A.MoraGeneral,0) AS DECIMAL(18,2)) AS VencidosBaseLegacy,
           CAST(A.ClientesVencidosCierre AS INT) AS ClientesVencidosBase,
           M.MetaCapitalCierre, M.MetaClientesActivosCierre,
           M.MetaVencidosMaximoCierre, M.MetaRecuperacionVencidosMes,
           CAST(CASE WHEN M.TipoCartera='ESPECIAL'
                          AND M.MetaVencidosMaximoCierre IS NOT NULL
                          AND M.MetaRecuperacionVencidosMes IS NOT NULL THEN 1
                     WHEN M.TipoCartera='PRODUCTIVA'
                          AND M.MetaCapitalCierre IS NOT NULL
                          AND M.MetaClientesActivosCierre IS NOT NULL
                          AND M.MetaVencidosMaximoCierre IS NOT NULL
                          AND M.MetaRecuperacionVencidosMes IS NOT NULL THEN 1
                     ELSE 0 END AS BIT) AS Configurada,
           @PeriodoCerrado AS PeriodoCerrado,
           CAST(CASE WHEN @Periodo=@PeriodoActual AND @Ahora>=@Periodo
                          AND @Ahora<=@FechaLimite AND @PeriodoCerrado=0
                     THEN 1 ELSE 0 END AS BIT) AS PuedeEditar,
           @FechaLimite AS FechaLimiteEdicion,
           M.Asesor,M.Supervisor,M.Mercado,M.Orden,M.TipoCartera
    FROM Cartera M
    INNER JOIN MAESTRO.Usuario U ON U.UsuarioId=M.UsuarioId
    LEFT JOIN MAESTRO.Persona P ON P.PersonaId=U.PersonaId
    LEFT JOIN Anterior A ON A.UsuarioId=M.UsuarioId
    ORDER BY M.Orden;
END;
GO


/* Consulta unificada: historico congelado o avance vivo del mes actual. */
CREATE OR ALTER PROCEDURE CREDITO.usp_ObtenerAvanceMetasGerenciales
    @Periodo DATE=NULL,
    @OficinaId INT=1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ahora DATETIME2(0)=CREDITO.ufn_FechaGerencial();
    DECLARE @FechaCorte DATE=CAST(@Ahora AS DATE);
    DECLARE @PeriodoActual DATE=DATEFROMPARTS(YEAR(@Ahora),MONTH(@Ahora),1);
    SET @Periodo=CASE WHEN @Periodo IS NULL THEN @PeriodoActual
                      ELSE DATEFROMPARTS(YEAR(@Periodo),MONTH(@Periodo),1) END;

    IF @Periodo>@PeriodoActual THROW 53030, 'No se puede consultar un periodo futuro.',1;

    IF @Periodo<@PeriodoActual
    BEGIN
        DECLARE @CierreId BIGINT,@FechaCierre DATETIME2(0);
        SELECT @CierreId=CierreGerencialId,@FechaCierre=FechaCierre
        FROM CREDITO.CierreGerencial WHERE Periodo=@Periodo AND Estado='CER';
        IF @CierreId IS NULL THROW 53031, 'No existe un cierre oficial para el periodo solicitado.',1;

        ;WITH Anterior AS
        (
            SELECT D.*
            FROM CREDITO.CierreGerencial C
            INNER JOIN CREDITO.CierreGerencialDetalle D
                ON D.CierreGerencialId=C.CierreGerencialId
            WHERE C.Periodo=DATEADD(MONTH,-1,@Periodo) AND C.Estado='CER'
        ),
        Datos AS
        (
            SELECT D.Orden,D.UsuarioId,D.NombreUsuario,D.NombreCompleto,
                   D.Asesor,D.Supervisor,D.Mercado,D.TipoCartera,
                   A.CapitalCierre AS CapitalBase,A.TotalClientesActivos AS ClientesBase,
                   CASE WHEN A.DefinicionVencidos=D.DefinicionVencidos
                        THEN COALESCE(A.VencidosCuotasCierre,A.MoraGeneral)
                        ELSE A.VencidosCuotasCierre END AS VencidosBaseComparable,
                   A.MoraGeneral AS VencidosBaseLegacy,
                   COALESCE(A.ClientesVencidosCierre,SP.NroClientesSaldoMoraCartera)
                        AS ClientesVencidosBase,
                   M.MetaCapitalCierre,M.MetaClientesActivosCierre,
                   M.MetaVencidosMaximoCierre,M.MetaRecuperacionVencidosMes,
                   D.CapitalCierre AS CapitalActual,
                   D.TotalClientesActivos AS ClientesActivosActual,
                   COALESCE(D.VencidosCuotasCierre,D.MoraGeneral) AS VencidosActual,
                   COALESCE(D.ClientesVencidosCierre,SA.NroClientesSaldoMoraCartera,0)
                        AS ClientesVencidosActual,
                   ISNULL(D.ClientesNuevosMes,0) AS ClientesNuevosMes
            FROM CREDITO.CierreGerencialDetalle D
            LEFT JOIN Anterior A ON A.UsuarioId=D.UsuarioId
            LEFT JOIN CREDITO.MetaGerencialAnalista M
                ON M.Periodo=@Periodo AND M.UsuarioId=D.UsuarioId AND M.Activo=1
            LEFT JOIN CREDITO.SaldoCarteraMensual SA
                ON SA.Anio=YEAR(@Periodo) AND SA.Mes=MONTH(@Periodo)
               AND SA.OficinaId=@OficinaId AND SA.AgenteId=D.UsuarioId
            LEFT JOIN CREDITO.SaldoCarteraMensual SP
                ON SP.Anio=YEAR(DATEADD(MONTH,-1,@Periodo))
               AND SP.Mes=MONTH(DATEADD(MONTH,-1,@Periodo))
               AND SP.OficinaId=@OficinaId AND SP.AgenteId=D.UsuarioId
            WHERE D.CierreGerencialId=@CierreId
        )
        SELECT D.Orden,D.UsuarioId,D.NombreUsuario,D.NombreCompleto,
               D.Asesor,D.Supervisor,D.Mercado,D.TipoCartera,@Periodo AS Periodo,
               CAST(D.CapitalBase AS DECIMAL(18,2)) AS CapitalBase,D.MetaCapitalCierre,
               CAST(D.CapitalActual AS DECIMAL(18,2)) AS CapitalActual,
               CAST(CASE WHEN D.MetaCapitalCierre IS NULL THEN NULL
                    ELSE D.CapitalActual-D.MetaCapitalCierre END AS DECIMAL(18,2)) AS DiferenciaCapital,
               CAST(CASE WHEN ISNULL(D.MetaCapitalCierre,0)=0 THEN NULL
                    ELSE D.CapitalActual*100.0/D.MetaCapitalCierre END AS DECIMAL(10,2)) AS CumplimientoCapitalPct,
               CASE WHEN D.TipoCartera='ESPECIAL' THEN 'NO APLICA'
                    WHEN D.MetaCapitalCierre IS NULL THEN 'SIN CONFIGURAR'
                    WHEN D.CapitalActual>=D.MetaCapitalCierre THEN 'CUMPLIDA'
                    ELSE 'NO CUMPLIDA' END AS EstadoCapital,
               D.ClientesBase,D.MetaClientesActivosCierre,D.ClientesActivosActual,
               CASE WHEN D.MetaClientesActivosCierre IS NULL THEN NULL
                    ELSE D.ClientesActivosActual-D.MetaClientesActivosCierre END AS DiferenciaClientes,
               CAST(CASE WHEN ISNULL(D.MetaClientesActivosCierre,0)=0 THEN NULL
                    ELSE D.ClientesActivosActual*100.0/D.MetaClientesActivosCierre END AS DECIMAL(10,2)) AS CumplimientoClientesPct,
               CASE WHEN D.TipoCartera='ESPECIAL' THEN 'NO APLICA'
                    WHEN D.MetaClientesActivosCierre IS NULL THEN 'SIN CONFIGURAR'
                    WHEN D.ClientesActivosActual>=D.MetaClientesActivosCierre THEN 'CUMPLIDA'
                    ELSE 'NO CUMPLIDA' END AS EstadoClientes,
               CAST(D.VencidosBaseComparable AS DECIMAL(18,2)) AS VencidosBaseComparable,
               CAST(D.VencidosBaseLegacy AS DECIMAL(18,2)) AS VencidosBaseLegacy,
               D.MetaVencidosMaximoCierre,CAST(D.VencidosActual AS DECIMAL(18,2)) AS VencidosActual,
               CAST(CASE WHEN D.MetaVencidosMaximoCierre IS NULL THEN NULL
                    ELSE D.MetaVencidosMaximoCierre-D.VencidosActual END AS DECIMAL(18,2)) AS MargenVencidos,
               CASE WHEN D.MetaVencidosMaximoCierre IS NULL THEN 'SIN CONFIGURAR'
                    WHEN D.VencidosActual<=D.MetaVencidosMaximoCierre THEN 'DENTRO DEL LIMITE'
                    ELSE 'LIMITE EXCEDIDO' END AS EstadoVencidos,
               D.ClientesVencidosBase,D.ClientesVencidosActual,
               CAST(CASE WHEN D.ClientesActivosActual=0 THEN 0
                    ELSE D.ClientesVencidosActual*100.0/D.ClientesActivosActual END AS DECIMAL(10,2))
                    AS ClientesVencidosPct,
               D.MetaRecuperacionVencidosMes,
               CAST(NULL AS DECIMAL(18,2)) AS RecuperacionVencidosActual,
               CASE WHEN D.MetaRecuperacionVencidosMes IS NULL THEN 'SIN CONFIGURAR'
                    WHEN D.VencidosBaseComparable IS NULL THEN 'SIN BASE COMPARABLE'
                    ELSE 'NO DISPONIBLE EN HISTORICO LEGACY' END AS EstadoRecuperacion,
               CAST(CASE WHEN D.TipoCartera='ESPECIAL'
                              AND D.MetaVencidosMaximoCierre IS NOT NULL
                              AND D.MetaRecuperacionVencidosMes IS NOT NULL THEN 1
                         WHEN D.TipoCartera='PRODUCTIVA'
                              AND D.MetaCapitalCierre IS NOT NULL
                              AND D.MetaClientesActivosCierre IS NOT NULL
                              AND D.MetaVencidosMaximoCierre IS NOT NULL
                              AND D.MetaRecuperacionVencidosMes IS NOT NULL THEN 1
                         ELSE 0 END AS BIT) AS MetaConfigurada,
               @FechaCierre AS FechaCalculo,CAST(0 AS BIT) AS AvanceNoOficial,
               D.ClientesNuevosMes
        FROM Datos D ORDER BY D.Orden;
        RETURN;
    END;

    ;WITH Anterior AS
    (
        SELECT D.UsuarioId,D.CapitalCierre,D.TotalClientesActivos,
               D.MoraGeneral AS VencidosBaseLegacy,
               D.VencidosCuotasCierre AS VencidosBaseComparable,
               D.ClientesVencidosCierre
        FROM CREDITO.CierreGerencial C
        INNER JOIN CREDITO.CierreGerencialDetalle D ON D.CierreGerencialId=C.CierreGerencialId
        WHERE C.Periodo=DATEADD(MONTH,-1,@Periodo) AND C.Estado='CER'
    ),
    Datos AS
    (
        SELECT M.Orden,M.UsuarioId,CAST(U.NombreUsuario AS VARCHAR(100)) AS NombreUsuario,
               CAST(P.NombreCompleto AS VARCHAR(250)) AS NombreCompleto,
               M.Asesor,M.Supervisor,M.Mercado,M.TipoCartera,
               A.CapitalCierre AS CapitalBase,A.TotalClientesActivos AS ClientesBase,
               A.VencidosBaseComparable,A.VencidosBaseLegacy,
               A.ClientesVencidosCierre AS ClientesVencidosBase,
               M.MetaCapitalCierre,M.MetaClientesActivosCierre,
               M.MetaVencidosMaximoCierre,M.MetaRecuperacionVencidosMes,
               ISNULL(X.CapitalActual,0) AS CapitalActual,
               ISNULL(X.ClientesActivosActual,0) AS ClientesActivosActual,
               ISNULL(X.VencidosActual,0) AS VencidosActual,
               ISNULL(X.ClientesVencidosActual,0) AS ClientesVencidosActual,
               R.RecuperacionVencidosActual,ISNULL(N.ClientesNuevosMes,0) AS ClientesNuevosMes
        FROM CREDITO.MetaGerencialAnalista M
        INNER JOIN MAESTRO.Usuario U ON U.UsuarioId=M.UsuarioId
        LEFT JOIN MAESTRO.Persona P ON P.PersonaId=U.PersonaId
        LEFT JOIN Anterior A ON A.UsuarioId=M.UsuarioId
        LEFT JOIN CREDITO.ufn_MetricasGerencialesActuales(@FechaCorte,@OficinaId) X
            ON X.UsuarioId=M.UsuarioId
        LEFT JOIN CREDITO.ufn_RecuperacionVencidosGerencial(@Periodo,@FechaCorte) R
            ON R.UsuarioId=M.UsuarioId
        LEFT JOIN CREDITO.ufn_ClientesNuevosGerenciales(@Periodo,@OficinaId) N
            ON N.UsuarioId=M.UsuarioId
        WHERE M.Periodo=@Periodo AND M.Activo=1
    )
    SELECT D.Orden,D.UsuarioId,D.NombreUsuario,D.NombreCompleto,
           D.Asesor,D.Supervisor,D.Mercado,D.TipoCartera,@Periodo AS Periodo,
           CAST(D.CapitalBase AS DECIMAL(18,2)) AS CapitalBase,D.MetaCapitalCierre,
           CAST(D.CapitalActual AS DECIMAL(18,2)) AS CapitalActual,
           CAST(CASE WHEN D.MetaCapitalCierre IS NULL THEN NULL
                ELSE D.CapitalActual-D.MetaCapitalCierre END AS DECIMAL(18,2)) AS DiferenciaCapital,
           CAST(CASE WHEN ISNULL(D.MetaCapitalCierre,0)=0 THEN NULL
                ELSE D.CapitalActual*100.0/D.MetaCapitalCierre END AS DECIMAL(10,2)) AS CumplimientoCapitalPct,
           CASE WHEN D.TipoCartera='ESPECIAL' THEN 'NO APLICA'
                WHEN D.MetaCapitalCierre IS NULL THEN 'SIN CONFIGURAR'
                WHEN D.CapitalActual>=D.MetaCapitalCierre THEN 'CUMPLIDA' ELSE 'PENDIENTE' END AS EstadoCapital,
           D.ClientesBase,D.MetaClientesActivosCierre,D.ClientesActivosActual,
           CASE WHEN D.MetaClientesActivosCierre IS NULL THEN NULL
                ELSE D.ClientesActivosActual-D.MetaClientesActivosCierre END AS DiferenciaClientes,
           CAST(CASE WHEN ISNULL(D.MetaClientesActivosCierre,0)=0 THEN NULL
                ELSE D.ClientesActivosActual*100.0/D.MetaClientesActivosCierre END AS DECIMAL(10,2)) AS CumplimientoClientesPct,
           CASE WHEN D.TipoCartera='ESPECIAL' THEN 'NO APLICA'
                WHEN D.MetaClientesActivosCierre IS NULL THEN 'SIN CONFIGURAR'
                WHEN D.ClientesActivosActual>=D.MetaClientesActivosCierre THEN 'CUMPLIDA' ELSE 'PENDIENTE' END AS EstadoClientes,
           CAST(D.VencidosBaseComparable AS DECIMAL(18,2)) AS VencidosBaseComparable,
           CAST(D.VencidosBaseLegacy AS DECIMAL(18,2)) AS VencidosBaseLegacy,
           D.MetaVencidosMaximoCierre,CAST(D.VencidosActual AS DECIMAL(18,2)) AS VencidosActual,
           CAST(CASE WHEN D.MetaVencidosMaximoCierre IS NULL THEN NULL
                ELSE D.MetaVencidosMaximoCierre-D.VencidosActual END AS DECIMAL(18,2)) AS MargenVencidos,
           CASE WHEN D.MetaVencidosMaximoCierre IS NULL THEN 'SIN CONFIGURAR'
                WHEN D.VencidosActual<=D.MetaVencidosMaximoCierre THEN 'DENTRO DEL LIMITE'
                ELSE 'LIMITE EXCEDIDO' END AS EstadoVencidos,
           D.ClientesVencidosBase,D.ClientesVencidosActual,
           CAST(CASE WHEN D.ClientesActivosActual=0 THEN 0
                ELSE D.ClientesVencidosActual*100.0/D.ClientesActivosActual END AS DECIMAL(10,2))
                AS ClientesVencidosPct,
           D.MetaRecuperacionVencidosMes,
           CAST(D.RecuperacionVencidosActual AS DECIMAL(18,2)) AS RecuperacionVencidosActual,
           CASE WHEN D.MetaRecuperacionVencidosMes IS NULL THEN 'SIN CONFIGURAR'
                WHEN D.VencidosBaseComparable IS NULL THEN 'SIN BASE COMPARABLE'
                WHEN D.RecuperacionVencidosActual>=D.MetaRecuperacionVencidosMes THEN 'CUMPLIDA'
                ELSE 'PENDIENTE' END AS EstadoRecuperacion,
           CAST(CASE WHEN D.TipoCartera='ESPECIAL'
                          AND D.MetaVencidosMaximoCierre IS NOT NULL
                          AND D.MetaRecuperacionVencidosMes IS NOT NULL THEN 1
                     WHEN D.TipoCartera='PRODUCTIVA'
                          AND D.MetaCapitalCierre IS NOT NULL
                          AND D.MetaClientesActivosCierre IS NOT NULL
                          AND D.MetaVencidosMaximoCierre IS NOT NULL
                          AND D.MetaRecuperacionVencidosMes IS NOT NULL THEN 1
                     ELSE 0 END AS BIT) AS MetaConfigurada,
           @Ahora AS FechaCalculo,CAST(1 AS BIT) AS AvanceNoOficial,D.ClientesNuevosMes
    FROM Datos D ORDER BY D.Orden;
END;
GO


CREATE OR ALTER PROCEDURE CREDITO.usp_ObtenerAvanceMetasGerenciales
    @Periodo DATE = NULL,
    @OficinaId INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Ahora DATETIME2(0) = CREDITO.ufn_FechaGerencial();
    DECLARE @PeriodoActual DATE = DATEFROMPARTS(YEAR(@Ahora), MONTH(@Ahora), 1);
    DECLARE @PeriodoNormalizado DATE =
        CASE WHEN @Periodo IS NULL THEN @PeriodoActual
             ELSE DATEFROMPARTS(YEAR(@Periodo), MONTH(@Periodo), 1) END;
    DECLARE @FechaCorte DATE =
        CASE WHEN @PeriodoNormalizado = @PeriodoActual
             THEN CAST(@Ahora AS DATE)
             ELSE EOMONTH(@PeriodoNormalizado) END;

    CREATE TABLE #Avance
    (
        Orden SMALLINT NULL,
        UsuarioId INT NOT NULL,
        NombreUsuario VARCHAR(100) NULL,
        NombreCompleto VARCHAR(250) NULL,
        Asesor VARCHAR(150) NULL,
        Supervisor VARCHAR(150) NULL,
        Mercado VARCHAR(150) NULL,
        TipoCartera VARCHAR(20) NULL,
        Periodo DATE NOT NULL,
        CapitalBase DECIMAL(18,2) NULL,
        MetaCapitalCierre DECIMAL(18,2) NULL,
        CapitalActual DECIMAL(18,2) NOT NULL,
        DiferenciaCapital DECIMAL(18,2) NULL,
        CumplimientoCapitalPct DECIMAL(10,2) NULL,
        EstadoCapital VARCHAR(40) NULL,
        ClientesBase INT NULL,
        MetaClientesActivosCierre INT NULL,
        ClientesActivosActual INT NOT NULL,
        DiferenciaClientes INT NULL,
        CumplimientoClientesPct DECIMAL(10,2) NULL,
        EstadoClientes VARCHAR(40) NULL,
        VencidosBaseComparable DECIMAL(18,2) NULL,
        VencidosBaseLegacy DECIMAL(18,2) NULL,
        MetaVencidosMaximoCierre DECIMAL(18,2) NULL,
        VencidosActual DECIMAL(18,2) NOT NULL,
        MargenVencidos DECIMAL(18,2) NULL,
        EstadoVencidos VARCHAR(50) NULL,
        ClientesVencidosBase INT NULL,
        ClientesVencidosActual INT NOT NULL,
        ClientesVencidosPct DECIMAL(10,2) NOT NULL,
        MetaRecuperacionVencidosMes DECIMAL(18,2) NULL,
        RecuperacionVencidosActual DECIMAL(18,2) NULL,
        EstadoRecuperacion VARCHAR(50) NULL,
        MetaConfigurada BIT NOT NULL,
        FechaCalculo DATETIME2(0) NOT NULL,
        AvanceNoOficial BIT NOT NULL,
        ClientesNuevosMes INT NOT NULL
    );

    INSERT INTO #Avance
    EXEC CREDITO.usp_ObtenerAvanceMetasGerencialesBase
        @Periodo = @PeriodoNormalizado,
        @OficinaId = @OficinaId;

    IF @PeriodoNormalizado < @PeriodoActual
    BEGIN
        SELECT B.*,
               CAST(ISNULL(D.MontoClientesNuevosMes, 0) AS DECIMAL(18,2))
                   AS MontoClientesNuevosMes,
               CAST(ISNULL(D.MontoCobradoMes, 0) AS DECIMAL(18,2)) AS MontoCobradoMes,
               CAST(ISNULL(D.DesembolsosMes, 0) AS DECIMAL(18,2)) AS DesembolsosMes,
               ISNULL(D.NroOperacionesMes, 0) AS NroOperacionesMes
        FROM #Avance B
        LEFT JOIN CREDITO.CierreGerencial C
            ON C.Periodo = @PeriodoNormalizado AND C.Estado = 'CER'
        LEFT JOIN CREDITO.CierreGerencialDetalle D
            ON D.CierreGerencialId = C.CierreGerencialId
           AND D.UsuarioId = B.UsuarioId
        ORDER BY B.Orden;
        RETURN;
    END;

    SELECT B.*,
           CAST(ISNULL(A.MontoClientesNuevosMes, 0) AS DECIMAL(18,2))
               AS MontoClientesNuevosMes,
           CAST(ISNULL(A.MontoCobradoMes, 0) AS DECIMAL(18,2)) AS MontoCobradoMes,
           CAST(ISNULL(A.DesembolsosMes, 0) AS DECIMAL(18,2)) AS DesembolsosMes,
           ISNULL(A.NroOperacionesMes, 0) AS NroOperacionesMes
    FROM #Avance B
    LEFT JOIN CREDITO.ufn_ActividadGerencialMensual(
        @PeriodoNormalizado, @FechaCorte, @OficinaId) A
        ON A.UsuarioId = B.UsuarioId
    ORDER BY B.Orden;
END;
GO


IF OBJECT_ID(N'CREDITO.trg_CierreGerencialDetalle_Actividad', N'TR') IS NOT NULL
    DROP TRIGGER CREDITO.trg_CierreGerencialDetalle_Actividad;
GO

/* Congela las metricas cuando el motor inserta un nuevo cierre. */
CREATE   TRIGGER CREDITO.trg_CierreGerencialDetalle_Actividad
ON CREDITO.CierreGerencialDetalle
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE D
       SET D.ClientesNuevosMes = A.ClientesNuevosMes,
           D.MontoClientesNuevosMes = A.MontoClientesNuevosMes,
           D.MontoCobradoMes = A.MontoCobradoMes,
           D.DesembolsosMes = A.DesembolsosMes,
           D.NroOperacionesMes = A.NroOperacionesMes
    FROM CREDITO.CierreGerencialDetalle D
    INNER JOIN inserted I
        ON I.CierreGerencialDetalleId = D.CierreGerencialDetalleId
    INNER JOIN CREDITO.CierreGerencial C
        ON C.CierreGerencialId = D.CierreGerencialId
    OUTER APPLY
    (
        SELECT X.ClientesNuevosMes,
               X.MontoClientesNuevosMes,
               X.MontoCobradoMes,
               X.DesembolsosMes,
               X.NroOperacionesMes
        FROM CREDITO.ufn_ActividadGerencialMensual(C.Periodo, C.FechaCorte, 1) X
        WHERE X.UsuarioId = D.UsuarioId
    ) A;
END;
GO

/* -------------------------------------------------------------------------- */
/* H) ÃNDICES DASHBOARD (rendimiento)                                         */
/* -------------------------------------------------------------------------- */
-- Índice opcional para el tablero del analista (cobranza por rango de FechaReg).
-- MovimientoCaja solo tiene IX por CreditoId+Operacion+Estado; el ranking y el
-- gráfico filtran por fecha y en el legado eso terminaba en timeout.
-- Ejecutar en ventana de mantenimiento. No es obligatorio para que el API funcione.

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_MovimientoCaja_FechaReg_CUO'
      AND object_id = OBJECT_ID(N'CREDITO.MovimientoCaja')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MovimientoCaja_FechaReg_CUO
        ON CREDITO.MovimientoCaja (FechaReg, CreditoId)
        INCLUDE (ImportePago)
        WHERE Operacion = 'CUO' AND Estado = 1 AND ImportePago > 0;
END
GO
GO

-- IX for dashboard admin filters by office
-- Ejecutar en ventana de mantenimiento. No es obligatorio para que el API funcione.

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Credito_Oficina_Estado_Desembolso'
      AND object_id = OBJECT_ID(N'CREDITO.Credito')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Credito_Oficina_Estado_Desembolso
        ON CREDITO.Credito (OficinaId, Estado, FechaDesembolso)
        INCLUDE (MontoDesembolso, UsuarioRegId, PersonaId, IndIrrecuperable, FechaVencimiento);
END
GO
GO

-- Índices para tablero gerencial (detalle/cartera por oficina).
-- Ejecutar en ventana de mantenimiento. Idempotente.

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PlanPago_Estado_CreditoId'
      AND object_id = OBJECT_ID(N'CREDITO.PlanPago')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PlanPago_Estado_CreditoId
        ON CREDITO.PlanPago (Estado, CreditoId)
        INCLUDE (Cuota, Cargo, PagoCuota, PagoLibre, FechaVencimiento);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_MovimientoCaja_Operacion_Estado_Fecha'
      AND object_id = OBJECT_ID(N'CREDITO.MovimientoCaja')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MovimientoCaja_Operacion_Estado_Fecha
        ON CREDITO.MovimientoCaja (Operacion, Estado, FechaReg)
        INCLUDE (CreditoId, ImportePago, IndEntrada, CajaDiarioId);
END
GO
GO

/* -------------------------------------------------------------------------- */
/* VERIFICACIÃ“N RÃPIDA                                                        */
/* -------------------------------------------------------------------------- */
SELECT
    COL_LENGTH(N'MAESTRO.Cliente', N'Latitud') AS ClienteLatitud,
    COL_LENGTH(N'CREDITO.Credito', N'EsPrendario') AS EsPrendario,
    OBJECT_ID(N'CREDITO.Prenda', N'U') AS Prenda,
    OBJECT_ID(N'CREDITO.CreditoCondonacion', N'U') AS CreditoCondonacion,
    OBJECT_ID(N'CREDITO.CierreGerencial', N'U') AS CierreGerencial,
    OBJECT_ID(N'CREDITO.usp_SolicitarCondonacion', N'P') AS usp_SolicitarCondonacion,
    OBJECT_ID(N'CREDITO.usp_RegistrarTransferenciaBancos', N'P') AS usp_RegistrarTransferenciaBancos,
    OBJECT_ID(N'CREDITO.usp_IntentarGenerarCierreGerencialMensual', N'P') AS usp_IntentarCierre,
    (
        SELECT CHARACTER_MAXIMUM_LENGTH
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = N'MAESTRO' AND TABLE_NAME = N'Usuario' AND COLUMN_NAME = N'ClaveUsuario'
    ) AS ClaveUsuarioLen;
GO

PRINT N'Cutover modern deltas OK.';
GO
