-- Tabla: [MAESTRO].[Ocupacion]

CREATE TABLE [MAESTRO].[Ocupacion] (
    [OcupacionId] int IDENTITY NOT NULL,
    [Denominacion] varchar(200) NULL,
    [Estado] bit NULL
);

-- Indices
--   PRIMARY KEY PK__Ocupacio__77075F7735FDC083 (CLUSTERED): [OcupacionId]
