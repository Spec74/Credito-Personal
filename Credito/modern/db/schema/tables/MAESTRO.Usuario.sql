-- Tabla: [MAESTRO].[Usuario]

CREATE TABLE [MAESTRO].[Usuario] (
    [UsuarioId] int IDENTITY NOT NULL,
    [PersonaId] int NOT NULL,
    [NombreUsuario] nvarchar(50) NOT NULL,
    [ClaveUsuario] nvarchar(50) NOT NULL,
    [Estado] bit NOT NULL
);

-- Indices
--   PRIMARY KEY PK__Usuario (CLUSTERED): [UsuarioId]

-- Claves foraneas
--   FK__Usuario__Persona__33C07256 : ([PersonaId]) -> [MAESTRO].[Persona] ([PersonaId])
