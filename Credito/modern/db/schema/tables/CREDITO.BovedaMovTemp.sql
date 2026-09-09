-- Tabla: [CREDITO].[BovedaMovTemp]

CREATE TABLE [CREDITO].[BovedaMovTemp] (
    [BovedaMovTempId] int IDENTITY NOT NULL,
    [BovedaInicioId] int NOT NULL,
    [BovedaDestinoId] int NOT NULL,
    [CodOperacion] char(3) NOT NULL,
    [Glosa] varchar(max) NULL,
    [Importe] decimal(16,2) NOT NULL,
    [UsuarioRegId] int NOT NULL,
    [MovimientoBovedaIniId] int NOT NULL,
    [FechaReg] datetime NOT NULL,
    [IndEntrada] bit NOT NULL,
    [Estado] bit NOT NULL
);

-- Indices
--   PRIMARY KEY PK__BovedaMo__C04A9CF60154EE1A (CLUSTERED): [BovedaMovTempId]

-- Claves foraneas
--   FK_BovedaMovTemp_Boveda : ([BovedaInicioId]) -> [CREDITO].[Boveda] ([BovedaId])
--   FK_BovedaMovTemp_Boveda1 : ([BovedaDestinoId]) -> [CREDITO].[Boveda] ([BovedaId])
