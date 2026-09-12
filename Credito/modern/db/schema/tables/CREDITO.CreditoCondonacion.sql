-- Tabla: [CREDITO].[CreditoCondonacion]

CREATE TABLE [CREDITO].[CreditoCondonacion] (
    [Id] int IDENTITY NOT NULL,
    [CreditoId] int NOT NULL,
    [CajaDiarioId] int NOT NULL,
    [Fecha] datetime NOT NULL,
    [MoraCondonacion] decimal(10,2) NOT NULL,
    [TotalPago] decimal(15,2) NOT NULL,
    [IndAprobado] bit NOT NULL
);

-- Indices
--   PRIMARY KEY PK_CreditoCondonacion (CLUSTERED): [Id]

-- Claves foraneas
--   FK_CreditoCondonacion_Credito : ([CreditoId]) -> [CREDITO].[Credito] ([CreditoId])
--   FK_CreditoCondonacion_CajaDiario : ([CajaDiarioId]) -> [CREDITO].[CajaDiario] ([CajaDiarioId])
