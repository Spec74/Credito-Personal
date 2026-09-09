-- Tabla: [ALMACEN].[TipoArticulo]

CREATE TABLE [ALMACEN].[TipoArticulo] (
    [TipoArticuloId] int IDENTITY NOT NULL,
    [Denominacion] varchar(100) NULL,
    [Descripcion] varchar(250) NULL,
    [IndTieneCodigo] bit NULL,
    [Estado] bit NOT NULL DEFAULT ((0)),
    [IndMovimientoAlmacen] bit NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__TipoArti__EA62AE60BB6EB207 (CLUSTERED): [TipoArticuloId]
