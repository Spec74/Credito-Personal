-- Tabla: [CREDITO].[Producto]

CREATE TABLE [CREDITO].[Producto] (
    [ProductoId] int IDENTITY NOT NULL,
    [Denominacion] varchar(255) NOT NULL,
    [InteresMinima] decimal(16,2) NOT NULL,
    [InteresMaxima] decimal(16,2) NOT NULL,
    [DiasGracia] int NOT NULL,
    [ImporteMoratorio] decimal(8,3) NOT NULL,
    [Estado] bit NOT NULL,
    [IndMora] bit NOT NULL DEFAULT ((1))
);

-- Indices
--   PRIMARY KEY PK__Producto__A430AEA325924E90 (CLUSTERED): [ProductoId]
