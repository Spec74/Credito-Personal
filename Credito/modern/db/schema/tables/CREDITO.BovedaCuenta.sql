-- Tabla: [CREDITO].[BovedaCuenta]

CREATE TABLE [CREDITO].[BovedaCuenta] (
    [BovedaCuentaId] int IDENTITY NOT NULL,
    [BovedaId] int NOT NULL,
    [TipoPagoId] smallint NULL DEFAULT ((1)),
    [SaldoInicial] decimal(15,2) NOT NULL
);

-- Indices
--   PRIMARY KEY PK__BovedaCu__5C0D65AB339CF883 (CLUSTERED): [BovedaCuentaId]

-- Claves foraneas
--   FK__BovedaCue__Boved__5A6F5FCC : ([BovedaId]) -> [CREDITO].[Boveda] ([BovedaId])
