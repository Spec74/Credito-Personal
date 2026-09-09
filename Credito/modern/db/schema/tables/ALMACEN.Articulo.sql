-- Tabla: [ALMACEN].[Articulo]

CREATE TABLE [ALMACEN].[Articulo] (
    [ArticuloId] int IDENTITY NOT NULL,
    [ModeloId] int NULL,
    [TipoArticuloId] int NULL,
    [CodArticulo] varchar(20) NULL,
    [Denominacion] varchar(200) NULL,
    [Descripcion] varchar(250) NULL,
    [Imagen] varchar(max) NULL,
    [IndPerecible] bit NULL DEFAULT ((0)),
    [IndImportado] bit NULL DEFAULT ((0)),
    [IndCanjeable] bit NULL,
    [Estado] bit NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__Articulo__C0D725ED1C880743 (CLUSTERED): [ArticuloId]

-- Claves foraneas
--   FK_ARTICULO_ModeloId : ([ModeloId]) -> [ALMACEN].[Modelo] ([ModeloId])
--   FK_ARTICULO_TipoArticuloId : ([TipoArticuloId]) -> [ALMACEN].[TipoArticulo] ([TipoArticuloId])
