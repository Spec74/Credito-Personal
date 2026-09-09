-- Tabla: [ALMACEN].[Movimiento]

CREATE TABLE [ALMACEN].[Movimiento] (
    [MovimientoId] int IDENTITY NOT NULL,
    [TipoMovimientoId] int NOT NULL,
    [AlmacenId] int NOT NULL,
    [Fecha] datetime NOT NULL,
    [SubTotal] decimal(16,2) NOT NULL DEFAULT ((0)),
    [IGV] decimal(16,2) NOT NULL DEFAULT ((0)),
    [AjusteRedondeo] decimal(16,2) NOT NULL DEFAULT ((0)),
    [TotalImporte] decimal(16,2) NOT NULL DEFAULT ((0)),
    [EstadoId] int NOT NULL,
    [Observacion] varchar(500) NULL,
    [Documento] varchar(50) NULL
);

-- Indices
--   PRIMARY KEY PK__Movimien__BF923C2C3C00B29C (CLUSTERED): [MovimientoId]

-- Claves foraneas
--   FK_ENTRADASALIDA_AlmacenId : ([AlmacenId]) -> [ALMACEN].[Almacen] ([AlmacenId])
--   FK_ENTRADASALIDA_TipoMovimientoId : ([TipoMovimientoId]) -> [ALMACEN].[TipoMovimiento] ([TipoMovimientoId])
