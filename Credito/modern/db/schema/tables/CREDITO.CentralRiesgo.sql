-- Tabla: [CREDITO].[CentralRiesgo]

CREATE TABLE [CREDITO].[CentralRiesgo] (
    [Anio] int NOT NULL,
    [Mes] int NOT NULL,
    [CreditoId] int NOT NULL,
    [DiasAtrazo] int NOT NULL DEFAULT ((0)),
    [CuotasAtrazo] int NOT NULL DEFAULT ((0)),
    [DeudaAtrazo] decimal(15,2) NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__CentralR__C59D72C748BC35D3 (CLUSTERED): [Anio], [Mes], [CreditoId]

-- Claves foraneas
--   FK__CentralRi__Credi__6E0D0F7C : ([CreditoId]) -> [CREDITO].[Credito] ([CreditoId])
