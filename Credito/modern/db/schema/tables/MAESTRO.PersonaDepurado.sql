-- Tabla: [MAESTRO].[PersonaDepurado]

CREATE TABLE [MAESTRO].[PersonaDepurado] (
    [PersonaDepuradoId] int IDENTITY NOT NULL,
    [PersonaId] int NOT NULL,
    [Descripcion] varchar(max) NOT NULL,
    [Estado] bit NOT NULL DEFAULT ((0)),
    [UsuarioRegId] int NOT NULL,
    [FechaReg] datetime NOT NULL
);

-- Indices
--   PRIMARY KEY PK__PersonaD__5B50EEA25C930030 (CLUSTERED): [PersonaDepuradoId]

-- Claves foraneas
--   FK__PersonaDe__Perso__57C7FD4B : ([PersonaId]) -> [MAESTRO].[Persona] ([PersonaId])
