-- Tabla: [MAESTRO].[TipoDocumento]

CREATE TABLE [MAESTRO].[TipoDocumento] (
    [TipoDocumentoId] int IDENTITY NOT NULL,
    [Denominacion] varchar(100) NOT NULL,
    [Descripcion] varchar(250) NULL,
    [IndVenta] bit NOT NULL DEFAULT ((0)),
    [IndAlmacen] bit NOT NULL DEFAULT ((0)),
    [IndAlmacenMov] bit NOT NULL DEFAULT ((0)),
    [IndCajaChica] bit NOT NULL DEFAULT ((0)),
    [Estado] bit NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__TipoDocu__A329EA870ACC3CD6 (CLUSTERED): [TipoDocumentoId]
