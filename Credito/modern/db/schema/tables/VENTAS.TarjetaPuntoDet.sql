-- Tabla: [VENTAS].[TarjetaPuntoDet]

CREATE TABLE [VENTAS].[TarjetaPuntoDet] (
    [TarjetaPuntoDetId] int IDENTITY NOT NULL,
    [TarjetaPuntoId] int NOT NULL,
    [OrdenVentaId] int NOT NULL,
    [ValorCanje] int NOT NULL
);

-- Indices
--   PRIMARY KEY PK__TarjetaP__AC9614BF1467C28E (CLUSTERED): [TarjetaPuntoDetId]

-- Claves foraneas
--   FK__TarjetaPu__Orden__17442F39 : ([OrdenVentaId]) -> [VENTAS].[OrdenVenta] ([OrdenVentaId])
--   FK__TarjetaPu__Tarje__16500B00 : ([TarjetaPuntoId]) -> [VENTAS].[TarjetaPunto] ([TarjetaPuntoId])
