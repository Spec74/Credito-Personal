-- Tabla: [CREDITO].[Boveda]

CREATE TABLE [CREDITO].[Boveda] (
    [BovedaId] int IDENTITY NOT NULL,
    [OficinaId] int NOT NULL,
    [SaldoInicial] decimal(16,2) NOT NULL DEFAULT ((0)),
    [Entradas] decimal(16,2) NOT NULL DEFAULT ((0)),
    [Salidas] decimal(16,2) NOT NULL DEFAULT ((0)),
    [SaldoFinal] decimal(16,2) NOT NULL DEFAULT ((0)),
    [FechaIniOperacion] datetime NOT NULL DEFAULT (getdate()),
    [FechaFinOperacion] datetime NULL,
    [IndCierre] bit NOT NULL DEFAULT ((0)),
    [IndTemporal] bit NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__Boveda__A7FEF717027E1C7D (CLUSTERED): [BovedaId]

-- Claves foraneas
--   FK__Boveda__OficinaI__046664EF : ([OficinaId]) -> [MAESTRO].[Oficina] ([OficinaId])
