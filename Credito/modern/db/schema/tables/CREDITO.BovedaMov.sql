-- Tabla: [CREDITO].[BovedaMov]

CREATE TABLE [CREDITO].[BovedaMov] (
    [MovimientoBovedaId] int IDENTITY NOT NULL,
    [BovedaId] int NOT NULL,
    [CodOperacion] char(3) NOT NULL,
    [Glosa] varchar(max) NULL,
    [Importe] decimal(16,2) NOT NULL,
    [IndEntrada] bit NOT NULL DEFAULT ((0)),
    [Estado] bit NOT NULL,
    [CajaDiarioId] int NULL,
    [UsuarioRegId] int NOT NULL,
    [FechaReg] datetime NOT NULL,
    [TipoPagoId] smallint NOT NULL DEFAULT ((1))
);

-- Indices
--   PRIMARY KEY PK_MovimientoBoveda (CLUSTERED): [MovimientoBovedaId]

-- Claves foraneas
--   FK_MovimientoBoveda_Boveda : ([BovedaId]) -> [CREDITO].[Boveda] ([BovedaId])
