-- Tabla: [CREDITO].[Caja]

CREATE TABLE [CREDITO].[Caja] (
    [CajaId] int IDENTITY NOT NULL,
    [OficinaId] int NOT NULL,
    [Denominacion] varchar(100) NOT NULL,
    [Estado] bit NOT NULL,
    [UsuarioRegId] int NOT NULL,
    [FechaReg] datetime NOT NULL,
    [IndAbierto] bit NOT NULL DEFAULT ((0)),
    [UsuarioModId] int NULL,
    [FechaMod] datetime NULL,
    [CajeroId] int NULL
);

-- Indices
--   PRIMARY KEY PK__Caja__A74F87070742D19A (CLUSTERED): [CajaId]

-- Claves foraneas
--   FK__Caja__OficinaId__092B1A0C : ([OficinaId]) -> [MAESTRO].[Oficina] ([OficinaId])
--   FK_Caja_Usuario : ([UsuarioRegId]) -> [MAESTRO].[Usuario] ([UsuarioId])
--   FK_Caja_Usuario1 : ([UsuarioModId]) -> [MAESTRO].[Usuario] ([UsuarioId])
