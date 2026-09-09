-- Tabla: [ALMACEN].[Almacen]

CREATE TABLE [ALMACEN].[Almacen] (
    [AlmacenId] int IDENTITY NOT NULL,
    [OficinaId] int NULL,
    [Denominacion] varchar(100) NULL,
    [Descripcion] varchar(250) NULL,
    [IndEstadoApertura] bit NULL,
    [FechaApertura] datetime NULL,
    [Estado] bit NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__Almacen__022A087607D6A0D6 (CLUSTERED): [AlmacenId]

-- Claves foraneas
--   FK_ALMACEN_OficinaId : ([OficinaId]) -> [MAESTRO].[Oficina] ([OficinaId])
