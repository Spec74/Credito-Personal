-- Tabla: [CREDITO].[PlanPago]

CREATE TABLE [CREDITO].[PlanPago] (
    [PlanPagoId] int IDENTITY NOT NULL,
    [CreditoId] int NOT NULL,
    [Numero] int NOT NULL,
    [Capital] decimal(16,2) NOT NULL,
    [FechaVencimiento] date NOT NULL,
    [Amortizacion] decimal(16,2) NOT NULL,
    [Interes] decimal(16,2) NOT NULL,
    [GastosAdm] decimal(16,2) NOT NULL,
    [Cuota] decimal(16,2) NOT NULL,
    [Estado] char(3) NOT NULL DEFAULT ((0)),
    [DiasAtrazo] int NOT NULL DEFAULT ((0)),
    [ImporteMora] decimal(16,2) NOT NULL DEFAULT ((0)),
    [Descuento] decimal(16,2) NOT NULL DEFAULT ((0)),
    [PagoCuota] decimal(16,2) NULL,
    [FechaPagoCuota] datetime NULL,
    [MovimientoCajaId] int NULL,
    [UsuarioModId] int NULL,
    [FechaMod] datetime NULL,
    [PagoLibre] decimal(16,2) NOT NULL DEFAULT ((0)),
    [Cargo] decimal(16,2) NOT NULL DEFAULT ((0))
);

-- Indices
--   INDEX IX_PlanPago_Cred_Estado_FechaVen (NONCLUSTERED): [CreditoId], [Estado], [FechaVencimiento] INCLUDE ([Cuota], [Cargo], [PagoLibre], [Numero])
--   INDEX IX_PlanPago_CreditoId (NONCLUSTERED): [CreditoId]
--   PRIMARY KEY PK__PlanPago__D534AF6F7370E317 (CLUSTERED): [PlanPagoId]

-- Claves foraneas
--   FK__PlanPago__Credit__75592B89 : ([CreditoId]) -> [CREDITO].[Credito] ([CreditoId])
--   FK_PlanPago_MovimientoCaja : ([MovimientoCajaId]) -> [CREDITO].[MovimientoCaja] ([MovimientoCajaId])
--   FK_PlanPago_Usuario : ([UsuarioModId]) -> [MAESTRO].[Usuario] ([UsuarioId])
