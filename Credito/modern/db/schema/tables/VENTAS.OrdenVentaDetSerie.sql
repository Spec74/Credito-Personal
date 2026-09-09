-- Tabla: [VENTAS].[OrdenVentaDetSerie]

CREATE TABLE [VENTAS].[OrdenVentaDetSerie] (
    [OrdenVentaDetSerieId] int IDENTITY NOT NULL,
    [OrdenVentaDetId] int NULL,
    [SerieArticuloId] int NULL
);

-- Indices
--   PRIMARY KEY PK__OrdenVen__8CB6EF5D06B8C1B5 (CLUSTERED): [OrdenVentaDetSerieId]

-- Claves foraneas
--   FK__OrdenVent__Orden__08A10A27 : ([OrdenVentaDetId]) -> [VENTAS].[OrdenVentaDet] ([OrdenVentaDetId])
--   FK__OrdenVent__Serie__09952E60 : ([SerieArticuloId]) -> [ALMACEN].[SerieArticulo] ([SerieArticuloId])
