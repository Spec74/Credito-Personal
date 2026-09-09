-- Tabla: [CREDITO].[Prenda]

CREATE TABLE [CREDITO].[Prenda] (
    [PrendaId] bigint IDENTITY NOT NULL,
    [CreditoId] int NOT NULL,
    [Descripcion] nvarchar(500) NOT NULL,
    [Marca] nvarchar(100) NULL,
    [Modelo] nvarchar(100) NULL,
    [Serie] nvarchar(100) NULL,
    [Color] nvarchar(50) NULL,
    [ValorTasacion] decimal(18,2) NOT NULL,
    [Observaciones] nvarchar(max) NULL,
    [FotoPath] nvarchar(500) NULL,
    [Estado] nvarchar(50) NOT NULL DEFAULT ('EN CUSTODIA'),
    [FechaRegistro] datetime NOT NULL DEFAULT (getdate()),
    [CodigoInterno] nvarchar(50) NULL,
    [UsuarioRegId] int NULL,
    [UsuarioModId] int NULL,
    [FechaMod] datetime NULL
);

-- Indices
--   INDEX IX_Prenda_CreditoId (NONCLUSTERED): [CreditoId]
--   INDEX IX_Prenda_Estado (NONCLUSTERED): [Estado]
--   PRIMARY KEY PK_Prenda (CLUSTERED): [PrendaId]

-- Claves foraneas
--   FK_Prenda_Credito : ([CreditoId]) -> [CREDITO].[Credito] ([CreditoId])
