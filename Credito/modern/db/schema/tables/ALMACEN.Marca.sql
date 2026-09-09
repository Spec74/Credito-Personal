-- Tabla: [ALMACEN].[Marca]

CREATE TABLE [ALMACEN].[Marca] (
    [MarcaId] int IDENTITY NOT NULL,
    [Denominacion] varchar(100) NULL,
    [Estado] bit NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__Marca__D5B1CD8BD31A7AF1 (CLUSTERED): [MarcaId]
