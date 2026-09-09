-- Tabla: [ALMACEN].[Modelo]

CREATE TABLE [ALMACEN].[Modelo] (
    [ModeloId] int IDENTITY NOT NULL,
    [Denominacion] varchar(70) NULL,
    [MarcaId] int NULL,
    [Estado] bit NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__Modelo__FA60529A096B469C (CLUSTERED): [ModeloId]

-- Claves foraneas
--   FK_MODELO_MarcaId : ([MarcaId]) -> [ALMACEN].[Marca] ([MarcaId])
