-- Tabla: [ALMACEN].[TransferenciaSerie]

CREATE TABLE [ALMACEN].[TransferenciaSerie] (
    [TransferenciaSerieId] int IDENTITY NOT NULL,
    [TransferenciaId] int NOT NULL,
    [SerieArticuloId] int NOT NULL
);

-- Indices
--   PRIMARY KEY PK__Transfer__055CBFF3F610697F (CLUSTERED): [TransferenciaSerieId]

-- Claves foraneas
--   FK__Transfere__Serie__382F5661 : ([SerieArticuloId]) -> [ALMACEN].[SerieArticulo] ([SerieArticuloId])
--   FK__Transfere__Trans__39237A9A : ([TransferenciaId]) -> [ALMACEN].[Transferencia] ([TransferenciaId])
