-- Tabla: [CREDITO].[Cargo]

CREATE TABLE [CREDITO].[Cargo] (
    [CargoId] int IDENTITY NOT NULL,
    [CreditoId] int NOT NULL,
    [NumCuota] int NOT NULL,
    [TipoCargoT2] int NOT NULL,
    [Importe] decimal(16,2) NOT NULL DEFAULT ((0)),
    [Descripcion] varchar(max) NOT NULL,
    [Estado] char(3) NOT NULL,
    [UsuarioId] int NOT NULL,
    [Fecha] datetime NOT NULL
);

-- Indices
--   PRIMARY KEY PK__Cargo__B4E665CD0EAEE938 (CLUSTERED): [CargoId]

-- Claves foraneas
--   FK__Cargo__CreditoId__109731AA : ([CreditoId]) -> [CREDITO].[Credito] ([CreditoId])
--   FK_Cargo_Usuario : ([UsuarioId]) -> [MAESTRO].[Usuario] ([UsuarioId])
