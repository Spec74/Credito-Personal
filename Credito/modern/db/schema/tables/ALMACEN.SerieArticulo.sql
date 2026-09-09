-- Tabla: [ALMACEN].[SerieArticulo]

CREATE TABLE [ALMACEN].[SerieArticulo] (
    [SerieArticuloId] int IDENTITY NOT NULL,
    [NumeroSerie] varchar(20) NOT NULL,
    [AlmacenId] int NOT NULL,
    [ArticuloId] int NOT NULL,
    [EstadoId] int NOT NULL,
    [MovimientoDetEntId] int NULL,
    [MovimientoDetSalId] int NULL
);

-- Indices
--   PRIMARY KEY PK__SerieArt__A6750CD55F49EED9 (CLUSTERED): [SerieArticuloId]

-- Claves foraneas
--   FK_SERIEARTICULO_AlmacenId : ([AlmacenId]) -> [ALMACEN].[Almacen] ([AlmacenId])
--   FK_SERIEARTICULO_ArticuloId : ([ArticuloId]) -> [ALMACEN].[Articulo] ([ArticuloId])
--   FK_SERIEARTICULO_MOVIMIENTODET : ([MovimientoDetEntId]) -> [ALMACEN].[MovimientoDet] ([MovimientoDetId])
