-- Tabla: [ALMACEN].[MovimientoDet]

CREATE TABLE [ALMACEN].[MovimientoDet] (
    [MovimientoDetId] int IDENTITY NOT NULL,
    [MovimientoId] int NOT NULL,
    [ArticuloId] int NOT NULL,
    [Cantidad] int NOT NULL DEFAULT ((0)),
    [Descripcion] varchar(max) NULL,
    [PrecioUnitario] decimal(16,2) NOT NULL DEFAULT ((0)),
    [Descuento] decimal(16,2) NOT NULL DEFAULT ((0)),
    [Importe] decimal(16,2) NOT NULL DEFAULT ((0)),
    [IndCorrelativo] bit NOT NULL,
    [UnidadMedidaT10] int NULL
);

-- Indices
--   PRIMARY KEY PK__Movimien__C5252D9B41B98BF2 (CLUSTERED): [MovimientoDetId]

-- Claves foraneas
--   FK_DETENTRADASALIDA_ArticuloId : ([ArticuloId]) -> [ALMACEN].[Articulo] ([ArticuloId])
--   FK_DETENTRADASALIDA_EntradaSalidaId : ([MovimientoId]) -> [ALMACEN].[Movimiento] ([MovimientoId])
