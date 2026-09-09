-- Tabla: [VENTAS].[ListaPrecio]

CREATE TABLE [VENTAS].[ListaPrecio] (
    [ListaPrecioId] int IDENTITY NOT NULL,
    [ArticuloId] int NULL,
    [Monto] decimal(16,2) NULL,
    [Descuento] decimal(16,2) NULL,
    [Estado] bit NOT NULL DEFAULT ((0)),
    [Puntos] int NULL,
    [PuntosCanje] int NULL
);

-- Indices
--   PRIMARY KEY PK__ListaPre__44C04A8F5C036DDA (CLUSTERED): [ListaPrecioId]

-- Claves foraneas
--   FK_ListaPrecio_Articulo : ([ArticuloId]) -> [ALMACEN].[Articulo] ([ArticuloId])
