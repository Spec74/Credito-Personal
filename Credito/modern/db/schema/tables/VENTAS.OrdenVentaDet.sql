-- Tabla: [VENTAS].[OrdenVentaDet]

CREATE TABLE [VENTAS].[OrdenVentaDet] (
    [OrdenVentaDetId] int IDENTITY NOT NULL,
    [OrdenVentaId] int NOT NULL,
    [ArticuloId] int NOT NULL,
    [Cantidad] int NOT NULL,
    [Descripcion] varchar(max) NOT NULL,
    [ValorVenta] decimal(16,4) NOT NULL,
    [Descuento] decimal(16,4) NOT NULL,
    [Subtotal] decimal(16,4) NOT NULL,
    [Estado] bit NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__OrdenVen__A9F54CD367752086 (CLUSTERED): [OrdenVentaDetId]

-- Claves foraneas
--   FK_DETORDENVENTA_ArticuloId : ([ArticuloId]) -> [ALMACEN].[Articulo] ([ArticuloId])
--   FK_DETORDENVENTA_OrdenVentaId : ([OrdenVentaId]) -> [VENTAS].[OrdenVenta] ([OrdenVentaId])
