-- Tabla: [ALMACEN].[MovimientoDoc]

CREATE TABLE [ALMACEN].[MovimientoDoc] (
    [MovimientoDocId] int IDENTITY NOT NULL,
    [MovimientoId] int NULL,
    [TipoDocumentoId] int NULL,
    [SerieDocumento] varchar(12) NULL,
    [NroDocumento] varchar(12) NULL,
    [RemitenteId] int NULL,
    [DestinatarioId] int NULL,
    [DestinoRef] varchar(150) NULL
);

-- Indices
--   PRIMARY KEY PK__Movimien__0DB53331D8D6D84B (CLUSTERED): [MovimientoDocId]

-- Claves foraneas
--   FK_DOCENTRADASALIDA_DestinatarioId : ([DestinatarioId]) -> [MAESTRO].[Persona] ([PersonaId])
--   FK_DOCENTRADASALIDA_MovimientoId : ([MovimientoId]) -> [ALMACEN].[Movimiento] ([MovimientoId])
--   FK_DOCENTRADASALIDA_RemitenteId : ([RemitenteId]) -> [MAESTRO].[Persona] ([PersonaId])
--   FK_DOCENTRADASALIDA_TipoDocumentoId : ([TipoDocumentoId]) -> [MAESTRO].[TipoDocumento] ([TipoDocumentoId])
