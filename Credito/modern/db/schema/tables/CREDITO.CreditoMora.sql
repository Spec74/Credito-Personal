-- Tabla: [CREDITO].[CreditoMora]

CREATE TABLE [CREDITO].[CreditoMora] (
    [CreditoMoraId] int IDENTITY NOT NULL,
    [CreditoId] int NOT NULL,
    [MovimientoCajaId] int NULL,
    [Fecha] datetime NOT NULL,
    [Mora] decimal(10,2) NOT NULL DEFAULT ((0)),
    [DiasAtrazo] int NOT NULL DEFAULT ((0)),
    [SaldoMora] decimal(10,2) NOT NULL DEFAULT ((0)),
    [InteresMora] decimal(10,2) NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__CreditoM__946224AA1A76816E (CLUSTERED): [CreditoMoraId]

-- Claves foraneas
--   FK__CreditoMo__Credi__17786E0C : ([CreditoId]) -> [CREDITO].[Credito] ([CreditoId])
--   FK__CreditoMo__Movim__186C9245 : ([MovimientoCajaId]) -> [CREDITO].[MovimientoCaja] ([MovimientoCajaId])
