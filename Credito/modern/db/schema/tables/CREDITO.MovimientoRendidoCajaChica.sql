-- Tabla: [CREDITO].[MovimientoRendidoCajaChica]

CREATE TABLE [CREDITO].[MovimientoRendidoCajaChica] (
    [Id] int IDENTITY NOT NULL,
    [MovimientoCajaChicaId] int NOT NULL,
    [TipoDocumentoId] int NOT NULL,
    [Fecha] date NOT NULL,
    [Serie] varchar(10) NULL,
    [Numero] varchar(10) NULL,
    [RUC] varchar(11) NULL,
    [RazonSocial] varchar(254) NULL,
    [DetalleGasto] varchar(254) NOT NULL,
    [Importe] decimal(15,2) NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__Movimien__3214EC0763F98E8C (CLUSTERED): [Id]

-- Claves foraneas
--   FK__Movimient__Movim__75035A77 : ([MovimientoCajaChicaId]) -> [CREDITO].[MovimientoCajaChica] ([Id])
--   FK_MovimientoRendidoCajaChica_TipoDocumento : ([TipoDocumentoId]) -> [MAESTRO].[TipoDocumento] ([TipoDocumentoId])
