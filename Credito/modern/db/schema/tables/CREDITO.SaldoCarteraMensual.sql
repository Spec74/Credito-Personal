-- Tabla: [CREDITO].[SaldoCarteraMensual]

CREATE TABLE [CREDITO].[SaldoCarteraMensual] (
    [SaldoCarteraId] int IDENTITY NOT NULL,
    [OficinaId] int NOT NULL,
    [AgenteId] int NOT NULL,
    [Anio] int NOT NULL,
    [Mes] int NOT NULL,
    [FechaCierre] datetime NOT NULL,
    [NroDesembolsos] int NOT NULL DEFAULT ((0)),
    [MontoDesembolsos] decimal(15,2) NOT NULL DEFAULT ((0)),
    [SaldoCartera] decimal(15,2) NOT NULL DEFAULT ((0.0)),
    [NroClientesSaldoCartera] int NOT NULL DEFAULT ((0)),
    [SaldoMoraCartera] decimal(15,2) NOT NULL DEFAULT ((0.0)),
    [NroClientesSaldoMoraCartera] int NOT NULL DEFAULT ((0)),
    [SaldoVencido] decimal(15,2) NOT NULL DEFAULT ((0.0)),
    [SaldoMorosidad] decimal(15,2) NOT NULL DEFAULT ((0.0)),
    [NroClientesNuevos] int NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__SaldoCar__DC042B36EEEF29D3 (CLUSTERED): [SaldoCarteraId]
