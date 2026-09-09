-- Tabla: [MAESTRO].[Oficina]

CREATE TABLE [MAESTRO].[Oficina] (
    [OficinaId] int IDENTITY NOT NULL,
    [Denominacion] varchar(100) NULL,
    [Descripcion] varchar(250) NULL,
    [Telefono] varchar(20) NULL,
    [IndPrincipal] bit NOT NULL,
    [Estado] bit NOT NULL DEFAULT ((0)),
    [UsuarioAsignadoId] int NOT NULL DEFAULT ((3)),
    [Latitud] decimal(11,8) NULL,
    [Longitud] decimal(11,8) NULL
);

-- Indices
--   PRIMARY KEY PK__Oficina__E86F5FEC484BBBC8 (CLUSTERED): [OficinaId]

-- Claves foraneas
--   FK_Oficina_Usuario : ([UsuarioAsignadoId]) -> [MAESTRO].[Usuario] ([UsuarioId])
