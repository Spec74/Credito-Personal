-- Tabla: [MAESTRO].[Cliente]

CREATE TABLE [MAESTRO].[Cliente] (
    [ClienteId] int NOT NULL,
    [PersonaId] int NOT NULL,
    [ActividadEconId] int NULL,
    [Calificacion] char(1) NULL,
    [FechaRegistro] datetime NULL,
    [Estado] bit NOT NULL DEFAULT ((1)),
    [Nota] varchar(500) NULL,
    [DireccionNegocio] varchar(300) NULL,
    [DireccionNegocioRef] varchar(300) NULL,
    [AvalPersonaId] int NULL,
    [UsuarioRegId] int NOT NULL DEFAULT ((1)),
    [Bloqueado] bit NOT NULL DEFAULT ((0)),
    [TopeCredito] decimal(15,2) NULL,
    [ClasificacionRiesgoSBS] int NULL,
    [ClasificacionRiesgoSBSObs] varchar(500) NULL,
    [Latitud] decimal(11,8) NULL,
    [Longitud] decimal(11,8) NULL
);

-- Indices
--   PRIMARY KEY PK__Cliente__71ABD0877929BC6D (CLUSTERED): [ClienteId]

-- Claves foraneas
--   FK_Cliente_Persona : ([PersonaId]) -> [MAESTRO].[Persona] ([PersonaId])
