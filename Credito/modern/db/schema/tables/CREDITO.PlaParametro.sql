-- Tabla: [CREDITO].[PlaParametro]

CREATE TABLE [CREDITO].[PlaParametro] (
    [PlaParametroId] int NOT NULL,
    [IndividualUsd] decimal(18,2) NOT NULL,
    [MultipleUsd] decimal(18,2) NOT NULL,
    [UmbralIndividualMenorUsd] decimal(18,2) NULL,
    [UmbralMultipleMenorUsd] decimal(18,2) NULL,
    [TcVentaMesAnterior] decimal(12,4) NULL,
    [TcPeriodo] char(7) NULL,
    [FuenteNorma] varchar(80) NOT NULL,
    [UsuarioModId] int NULL,
    [FechaMod] datetime NULL
);

-- Indices
--   PRIMARY KEY PK_PlaParametro (CLUSTERED): [PlaParametroId]
