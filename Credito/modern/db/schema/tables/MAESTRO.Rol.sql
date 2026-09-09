-- Tabla: [MAESTRO].[Rol]

CREATE TABLE [MAESTRO].[Rol] (
    [RolId] int IDENTITY NOT NULL,
    [Denominacion] varchar(255) NULL,
    [Estado] bit NOT NULL
);

-- Indices
--   PRIMARY KEY PK__Rol__F92302F1615232DC (CLUSTERED): [RolId]
