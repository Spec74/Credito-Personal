/*
  Medios de pago por oficina (configurables + variantes).
  - Cada sucursal administra sus medios (teléfono, cuenta/CCI, QR EMV oficial).
  - TipoPagoId sigue siendo la clave de ledger (MAESTRO.ValorTabla TablaId=13).
  - Variante: ParentOficinaMedioPagoId apunta al medio padre.

  QR: según Circular BCRP 0003-2020 / EMVCo MPM, el payload debe ser el QR
  oficial del proveedor (Yape/Plin/banco). No inventar payloads.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'CREDITO.OficinaMedioPago', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.OficinaMedioPago (
        OficinaMedioPagoId       int            NOT NULL IDENTITY(1, 1),
        OficinaId                int            NOT NULL,
        ParentOficinaMedioPagoId int            NULL,
        TipoPagoId               smallint       NOT NULL,
        Nombre                   nvarchar(80)   NOT NULL,
        MarcaCodigo              nvarchar(32)   NOT NULL
            CONSTRAINT DF_OficinaMedioPago_Marca DEFAULT (N'otro'),
        Orden                    int            NOT NULL
            CONSTRAINT DF_OficinaMedioPago_Orden DEFAULT (100),
        Activo                   bit            NOT NULL
            CONSTRAINT DF_OficinaMedioPago_Activo DEFAULT (1),
        EsDigital                bit            NOT NULL
            CONSTRAINT DF_OficinaMedioPago_Digital DEFAULT (0),
        UsaCelular               bit            NOT NULL
            CONSTRAINT DF_OficinaMedioPago_Cel DEFAULT (0),
        Telefono                 nvarchar(20)   NULL,
        UsaCuenta                bit            NOT NULL
            CONSTRAINT DF_OficinaMedioPago_Cta DEFAULT (0),
        NumeroCuenta             nvarchar(40)   NULL,
        Cci                      nvarchar(30)   NULL,
        UsaQr                    bit            NOT NULL
            CONSTRAINT DF_OficinaMedioPago_Qr DEFAULT (0),
        QrEmvPayload             nvarchar(max)  NULL,
        QrNota                   nvarchar(200)  NULL,
        FamiliaCodigo            nvarchar(32)   NULL,
        SoloCentral              bit            NOT NULL
            CONSTRAINT DF_OficinaMedioPago_Central DEFAULT (0),
        CreatedAtUtc             datetime2(0)   NOT NULL
            CONSTRAINT DF_OficinaMedioPago_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc             datetime2(0)   NOT NULL
            CONSTRAINT DF_OficinaMedioPago_Updated DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_OficinaMedioPago PRIMARY KEY CLUSTERED (OficinaMedioPagoId),
        CONSTRAINT FK_OficinaMedioPago_Oficina
            FOREIGN KEY (OficinaId) REFERENCES MAESTRO.Oficina (OficinaId),
        CONSTRAINT FK_OficinaMedioPago_Parent
            FOREIGN KEY (ParentOficinaMedioPagoId)
            REFERENCES CREDITO.OficinaMedioPago (OficinaMedioPagoId),
        CONSTRAINT UQ_OficinaMedioPago_Oficina_Tipo
            UNIQUE (OficinaId, TipoPagoId)
    );

    CREATE INDEX IX_OficinaMedioPago_Oficina_Activo
        ON CREDITO.OficinaMedioPago (OficinaId, Activo, Orden);
END;
GO

/*
  Semilla idempotente por oficina.
  - Billeteras SoloCentral (10–13) solo en oficina principal (IndPrincipal=1).
  - Sucursales nuevas: el API también hace EnsureSemilla al listar/guardar.
*/
;WITH Catalogo AS (
    SELECT
        vt.ItemId AS TipoPagoId,
        LTRIM(RTRIM(vt.Denominacion)) AS Nombre,
        CASE
            WHEN vt.ItemId = 1 THEN N'efectivo'
            WHEN vt.ItemId IN (2, 10, 11) THEN N'yape'
            WHEN vt.ItemId IN (3, 12, 13) THEN N'interbank'
            WHEN vt.ItemId = 4 THEN N'bcp'
            WHEN vt.ItemId = 5 THEN N'banco-nacion'
            WHEN vt.ItemId = 9 THEN N'plin'
            ELSE N'otro'
        END AS MarcaCodigo,
        CASE
            WHEN vt.ItemId = 1 THEN 10
            WHEN vt.ItemId IN (10, 11) THEN 20
            WHEN vt.ItemId = 2 THEN 22
            WHEN vt.ItemId IN (12, 13) THEN 30
            WHEN vt.ItemId = 3 THEN 32
            WHEN vt.ItemId = 4 THEN 40
            WHEN vt.ItemId = 5 THEN 50
            WHEN vt.ItemId = 9 THEN 60
            ELSE 100 + vt.ItemId
        END AS Orden,
        CAST(CASE WHEN vt.ItemId > 1 THEN 1 ELSE 0 END AS bit) AS EsDigital,
        CAST(CASE WHEN vt.ItemId IN (2, 9, 10, 11) THEN 1 ELSE 0 END AS bit) AS UsaCelular,
        CAST(CASE WHEN vt.ItemId IN (3, 4, 5, 12, 13) THEN 1 ELSE 0 END AS bit) AS UsaCuenta,
        CAST(CASE WHEN vt.ItemId IN (2, 9, 10, 11) THEN 1 ELSE 0 END AS bit) AS UsaQr,
        CASE
            WHEN vt.ItemId IN (2, 10, 11) THEN N'YAPE'
            WHEN vt.ItemId IN (3, 12, 13) THEN N'INTERBANK'
            ELSE NULL
        END AS FamiliaCodigo,
        CAST(CASE WHEN vt.ItemId BETWEEN 10 AND 13 THEN 1 ELSE 0 END AS bit) AS SoloCentral
    FROM MAESTRO.ValorTabla AS vt
    WHERE vt.TablaId = 13
      AND vt.ItemId > 0
      AND vt.ItemId NOT BETWEEN 6 AND 8
)
INSERT INTO CREDITO.OficinaMedioPago (
    OficinaId, TipoPagoId, Nombre, MarcaCodigo, Orden, Activo,
    EsDigital, UsaCelular, UsaCuenta, UsaQr, FamiliaCodigo, SoloCentral
)
SELECT
    o.OficinaId,
    c.TipoPagoId,
    c.Nombre,
    c.MarcaCodigo,
    c.Orden,
    1,
    c.EsDigital,
    c.UsaCelular,
    c.UsaCuenta,
    c.UsaQr,
    c.FamiliaCodigo,
    c.SoloCentral
FROM MAESTRO.Oficina AS o
CROSS JOIN Catalogo AS c
WHERE o.Estado = 1
  AND (
        c.SoloCentral = 0
        OR ISNULL(o.IndPrincipal, 0) = 1
      )
  AND NOT EXISTS (
        SELECT 1
        FROM CREDITO.OficinaMedioPago AS m
        WHERE m.OficinaId = o.OficinaId
          AND m.TipoPagoId = c.TipoPagoId
      );
GO
