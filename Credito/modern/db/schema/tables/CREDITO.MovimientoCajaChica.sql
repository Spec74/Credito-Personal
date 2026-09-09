-- Tabla: [CREDITO].[MovimientoCajaChica]

CREATE TABLE [CREDITO].[MovimientoCajaChica] (
    [Id] int IDENTITY NOT NULL,
    [CajaChicaDiarioId] int NOT NULL,
    [PersonaId] int NOT NULL,
    [Operacion] char(3) NOT NULL,
    [Importe] decimal(15,2) NOT NULL DEFAULT ((0)),
    [Descripcion] varchar(254) NOT NULL,
    [IndEntrada] bit NOT NULL DEFAULT ((0)),
    [Estado] bit NOT NULL DEFAULT ((1)),
    [UsuarioRegId] int NOT NULL,
    [FechaReg] datetime NOT NULL,
    [IndRendido] bit NOT NULL DEFAULT ((0)),
    [ImporteRendido] decimal(15,2) NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__Movimien__3214EC07193D8554 (CLUSTERED): [Id]

-- Claves foraneas
--   FK__Movimient__CajaC__4FD1D5C8 : ([CajaChicaDiarioId]) -> [CREDITO].[CajaChicaDiario] ([Id])
--   FK__Movimient__Perso__50C5FA01 : ([PersonaId]) -> [MAESTRO].[Persona] ([PersonaId])
--   FK__Movimient__Usuar__53A266AC : ([UsuarioRegId]) -> [MAESTRO].[Usuario] ([UsuarioId])
