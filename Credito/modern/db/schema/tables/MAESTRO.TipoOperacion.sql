-- Tabla: [MAESTRO].[TipoOperacion]

CREATE TABLE [MAESTRO].[TipoOperacion] (
    [TipoOperacionId] int NOT NULL,
    [Codigo] char(3) NOT NULL,
    [Denominacion] varchar(50) NOT NULL,
    [IndEntrada] bit NOT NULL DEFAULT ((1)),
    [IndCajaDiario] bit NOT NULL DEFAULT ((0)),
    [IndBoveda] bit NOT NULL DEFAULT ((0)),
    [IndCajaChica] bit NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__TipoOper__72B493817CFA4D51 (CLUSTERED): [TipoOperacionId]
