-- Tabla: [CREDITO].[MovimientoCaja]

CREATE TABLE [CREDITO].[MovimientoCaja] (
    [MovimientoCajaId] int IDENTITY NOT NULL,
    [CajaDiarioId] int NOT NULL,
    [Operacion] char(3) NOT NULL,
    [ImportePago] decimal(16,2) NOT NULL DEFAULT ((0)),
    [PersonaId] int NULL,
    [TipoPagoId] int NOT NULL DEFAULT ((1)),
    [Descripcion] varchar(max) NULL,
    [IndEntrada] bit NOT NULL,
    [Estado] bit NOT NULL,
    [UsuarioRegId] int NOT NULL,
    [FechaReg] datetime NOT NULL,
    [OrdenVentaId] int NULL,
    [CreditoId] int NULL
);

-- Indices
--   INDEX IX_MovimientoCaja_Cred_Ope_Estado (NONCLUSTERED): [CreditoId], [Operacion], [Estado] INCLUDE ([ImportePago])
--   PRIMARY KEY PK__Movimien__266F555F11C0600D (CLUSTERED): [MovimientoCajaId]

-- Claves foraneas
--   FK__Movimient__CajaD__149CCCB8 : ([CajaDiarioId]) -> [CREDITO].[CajaDiario] ([CajaDiarioId])
--   FK__Movimient__Usuar__17793963 : ([UsuarioRegId]) -> [MAESTRO].[Usuario] ([UsuarioId])
--   FK_MovimientoCaja_Credito : ([CreditoId]) -> [CREDITO].[Credito] ([CreditoId])
--   FK_MovimientoCaja_OrdenVenta : ([OrdenVentaId]) -> [VENTAS].[OrdenVenta] ([OrdenVentaId])
--   FK_MovimientoCaja_Persona : ([PersonaId]) -> [MAESTRO].[Persona] ([PersonaId])
