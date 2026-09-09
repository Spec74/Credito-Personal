-- Tabla: [MAESTRO].[Persona]

CREATE TABLE [MAESTRO].[Persona] (
    [PersonaId] int IDENTITY NOT NULL,
    [Nombre] varchar(70) NOT NULL,
    [ApePaterno] varchar(70) NULL,
    [ApeMaterno] varchar(70) NULL,
    [NombreCompleto] varchar(250) NULL,
    [TipoDocumento] char(3) NOT NULL,
    [NumeroDocumento] varchar(12) NOT NULL,
    [Codigo] varchar(50) NULL,
    [Sexo] char(1) NULL,
    [TipoPersona] char(1) NOT NULL,
    [EmailPersonal] varchar(100) NULL,
    [FechaNacimiento] datetime NULL,
    [Direccion] varchar(300) NULL,
    [DireccionRef] varchar(300) NULL,
    [Estado] bit NOT NULL DEFAULT ((0)),
    [Celular1] varchar(10) NULL,
    [ConyuguePersonaId] int NULL,
    [TipoViviendaId] int NULL,
    [EstadoCivilId] int NULL,
    [DistritoId] int NULL
);

-- Indices
--   UNIQUE IX_Persona (NONCLUSTERED): [NumeroDocumento]
--   PRIMARY KEY PK__Persona__7C34D303AB4EC0D3 (CLUSTERED): [PersonaId]
