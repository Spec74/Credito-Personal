-- Tabla: [VENTAS].[TarjetaPunto]

CREATE TABLE [VENTAS].[TarjetaPunto] (
    [TarjetaPuntoId] int IDENTITY NOT NULL,
    [PersonaId] int NOT NULL,
    [TotalPuntos] int NOT NULL,
    [Estado] bit NOT NULL
);

-- Indices
--   PRIMARY KEY PK__TarjetaP__1D2E93810FA30D71 (CLUSTERED): [TarjetaPuntoId]

-- Claves foraneas
--   FK__TarjetaPu__Perso__118B55E3 : ([PersonaId]) -> [MAESTRO].[Persona] ([PersonaId])
