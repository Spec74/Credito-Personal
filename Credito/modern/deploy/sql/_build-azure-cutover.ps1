$ErrorActionPreference = 'Stop'
$extract = 'd:\Ebers\GitHub\Credito\modern\deploy\sql\_extract_cierre'
$out = 'd:\Ebers\GitHub\Credito\modern\deploy\sql\2026-09-23-azure-cutover-modern-deltas.sql'

function Convert-ToCreateOrAlter([string]$def) {
    $d = $def.Trim().TrimStart([char]0xFEFF)
    $d = [regex]::Replace($d, '(?im)^\s*CREATE\s+(OR\s+ALTER\s+)?PROCEDURE\b', 'CREATE OR ALTER PROCEDURE')
    $d = [regex]::Replace($d, '(?im)^\s*CREATE\s+(OR\s+ALTER\s+)?PROC\b', 'CREATE OR ALTER PROC')
    $d = [regex]::Replace($d, '(?im)^\s*CREATE\s+(OR\s+ALTER\s+)?FUNCTION\b', 'CREATE OR ALTER FUNCTION')
    return ($d.TrimEnd() + "`r`nGO`r`n")
}

function Append-FileRaw([System.Text.StringBuilder]$sb, [string]$path) {
    $raw = [System.IO.File]::ReadAllText($path)
    [void]$sb.AppendLine($raw.TrimEnd())
    [void]$sb.AppendLine('GO')
    [void]$sb.AppendLine()
}

function Append-CreateOrAlter([System.Text.StringBuilder]$sb, [string]$path) {
    $raw = [System.IO.File]::ReadAllText($path)
    [void]$sb.AppendLine((Convert-ToCreateOrAlter $raw))
    [void]$sb.AppendLine()
}

function Find-Extract([string]$name) {
    $p = Join-Path $extract "CREDITO.$name.sql"
    if (Test-Path $p) { return $p }
    $hit = Get-ChildItem $extract -Filter "*$name.sql" | Select-Object -First 1
    if ($null -eq $hit) { throw "No se encontro $name en $extract" }
    return $hit.FullName
}

$sb = New-Object System.Text.StringBuilder

[void]$sb.AppendLine(@'
/*
================================================================================
  Azure / restore cutover — deltas modernos sobre bak del cliente
================================================================================
  Uso:
    1) Restaurar el BAK/bacpac actual del cliente en Azure (reemplaza la DB vieja).
    2) Ejecutar ESTE script (idempotente) en la base restaurada.
    3) Apuntar Credito.Modern + strangler a esa connection string.

  Incluye:
    A) Geo Cliente/Oficina
    B) Prendario (columnas + Prenda + índices + menú + Rol ANALISTA)
    C) ClaveUsuario nvarchar(256) para PBKDF2
    D) Mora postergada (CreditoMora.MovimientoCajaId NULLABLE + usp_CreditoMora_*)
    E) Condonación (tabla + usp_SolicitarCondonacion)
    F) Bóveda transferencia entre bancos (usp_RegistrarTransferenciaBancos)
    G) Cierre gerencial (tablas + ufn_* + usp_* + trigger)
    H) Índices de rendimiento dashboard (recomendados)

  NO recrea el núcleo de negocio: eso viene en el restore.
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

'@)

[void]$sb.AppendLine('/* -------------------------------------------------------------------------- */')
[void]$sb.AppendLine('/* D) MORA POSTERGADA                                                          */')
[void]$sb.AppendLine('/* -------------------------------------------------------------------------- */')
Append-FileRaw $sb 'd:\Ebers\GitHub\Credito\modern\deploy\sql\2026-06-credito-mora-postergada.sql'

[void]$sb.AppendLine('/* -------------------------------------------------------------------------- */')
[void]$sb.AppendLine('/* E) CONDONACIÓN                                                              */')
[void]$sb.AppendLine('/* -------------------------------------------------------------------------- */')
Append-FileRaw $sb 'd:\Ebers\GitHub\Credito\modern\deploy\sql\2026-09-10-credito-condonacion.sql'

[void]$sb.AppendLine('/* -------------------------------------------------------------------------- */')
[void]$sb.AppendLine('/* F) BÓVEDA TRANSFERENCIA ENTRE BANCOS                                        */')
[void]$sb.AppendLine('/* -------------------------------------------------------------------------- */')
Append-FileRaw $sb 'd:\Ebers\GitHub\Credito\modern\deploy\sql\2026-09-10-boveda-transferencia-bancos.sql'

[void]$sb.AppendLine(@'
/* -------------------------------------------------------------------------- */
/* G) CIERRE GERENCIAL                                                        */
/* -------------------------------------------------------------------------- */
'@)

Append-CreateOrAlter $sb (Find-Extract 'ufn_FechaGerencial')

[void]$sb.AppendLine(@'
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

'@)

foreach ($n in @(
    'ufn_SaldosCuotasGerenciales',
    'ufn_ClientesNuevosGerenciales',
    'ufn_ActividadGerencialMensual',
    'ufn_MetricasGerencialesActuales',
    'ufn_RecuperacionVencidosGerencial'
)) {
    Append-CreateOrAlter $sb (Find-Extract $n)
}

foreach ($n in @(
    'usp_GenerarCierreGerencialMensual',
    'usp_IntentarGenerarCierreGerencialMensual',
    'usp_GuardarMetaGerencialDefinitiva',
    'usp_ListarMetasGerencialesDefinitivas',
    'usp_ObtenerAvanceMetasGerencialesBase',
    'usp_ObtenerAvanceMetasGerenciales'
)) {
    Append-CreateOrAlter $sb (Find-Extract $n)
}

[void]$sb.AppendLine(@'
IF OBJECT_ID(N'CREDITO.trg_CierreGerencialDetalle_Actividad', N'TR') IS NOT NULL
    DROP TRIGGER CREDITO.trg_CierreGerencialDetalle_Actividad;
GO
'@)
[void]$sb.AppendLine(([System.IO.File]::ReadAllText((Join-Path $extract 'trg_CierreGerencialDetalle_Actividad.sql'))).TrimEnd())
[void]$sb.AppendLine('GO')
[void]$sb.AppendLine()

[void]$sb.AppendLine('/* -------------------------------------------------------------------------- */')
[void]$sb.AppendLine('/* H) ÍNDICES DASHBOARD (rendimiento)                                         */')
[void]$sb.AppendLine('/* -------------------------------------------------------------------------- */')
foreach ($f in @(
    '2026-09-10-dashboard-analista-ix-movimientocaja.sql',
    '2026-09-16-dashboard-admin-ix-credito-oficina.sql',
    '2026-09-16-dashboard-admin-ix-planpago-pen.sql'
)) {
    Append-FileRaw $sb ("d:\Ebers\GitHub\Credito\modern\deploy\sql\$f")
}

[void]$sb.AppendLine(@'
/* -------------------------------------------------------------------------- */
/* VERIFICACIÓN RÁPIDA                                                        */
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
'@)

[System.IO.File]::WriteAllText($out, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Output ("WROTE {0} bytes={1} lines={2}" -f $out, (Get-Item $out).Length, (Get-Content $out | Measure-Object -Line).Lines)
