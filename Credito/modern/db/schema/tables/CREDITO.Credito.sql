-- Tabla: [CREDITO].[Credito]

CREATE TABLE [CREDITO].[Credito] (
    [CreditoId] int IDENTITY NOT NULL,
    [PersonaId] int NOT NULL,
    [ProductoId] int NOT NULL,
    [Descripcion] varchar(max) NOT NULL,
    [MontoProducto] decimal(16,2) NOT NULL,
    [MontoInicial] decimal(16,2) NOT NULL,
    [MontoCredito] decimal(16,2) NOT NULL,
    [MontoGastosAdm] decimal(16,2) NOT NULL,
    [CentralRiesgo] decimal(16,2) NOT NULL DEFAULT ((0)),
    [MontoDesembolso] decimal(16,2) NOT NULL DEFAULT ((0)),
    [TipoGastoAdm] char(3) NOT NULL DEFAULT ('CUO'),
    [FormaPago] char(1) NOT NULL,
    [NumeroCuotas] int NOT NULL,
    [Interes] decimal(4,2) NOT NULL,
    [FechaPrimerPago] date NOT NULL,
    [FechaAprobacion] datetime NULL,
    [FechaDesembolso] datetime NULL,
    [Observacion] varchar(max) NULL,
    [Estado] char(3) NOT NULL,
    [OficinaId] int NOT NULL DEFAULT ((1)),
    [FechaVencimiento] date NOT NULL DEFAULT (getdate()),
    [FechaPagado] datetime NULL,
    [OrdenVentaId] int NULL,
    [TipoCuota] char(1) NOT NULL DEFAULT ('V'),
    [Calificacion] char(1) NOT NULL DEFAULT ('A'),
    [IndCondonacion] bit NOT NULL DEFAULT ((0)),
    [MontoCondonacion] decimal(16,2) NOT NULL DEFAULT ((0)),
    [PersonaAvalId] int NULL,
    [UsuarioRegId] int NOT NULL,
    [FechaReg] datetime NOT NULL,
    [UsuarioModId] int NULL,
    [FechaMod] datetime NULL,
    [IndIrrecuperable] bit NOT NULL DEFAULT ((0)),
    [EsPrendario] bit NOT NULL DEFAULT ((0)),
    [MontoTasacion] decimal(18,2) NULL,
    [NumeroContratoPrendario] nvarchar(50) NULL,
    [FechaRemate] date NULL,
    [PrendaId] bigint NULL
);

-- Indices
--   INDEX IX_Credito_EsPrendario (NONCLUSTERED): [EsPrendario]
--   INDEX IX_Credito_Estado_Ofi_Usuario (NONCLUSTERED): [Estado], [OficinaId], [UsuarioRegId] INCLUDE ([PersonaId], [MontoCredito], [Interes], [FechaPrimerPago], [FechaVencimiento], [FormaPago])
--   PRIMARY KEY PK__Credito__4FE406DD6FA05233 (CLUSTERED): [CreditoId]

-- Claves foraneas
--   FK__Credito__OrdenVe__3E5DFC5A : ([OrdenVentaId]) -> [VENTAS].[OrdenVenta] ([OrdenVentaId])
--   FK_Credito_Oficina : ([OficinaId]) -> [MAESTRO].[Oficina] ([OficinaId])
--   FK_Credito_Persona : ([PersonaId]) -> [MAESTRO].[Persona] ([PersonaId])
--   FK_Credito_Producto : ([ProductoId]) -> [CREDITO].[Producto] ([ProductoId])
--   FK_Credito_Usuario3 : ([UsuarioRegId]) -> [MAESTRO].[Usuario] ([UsuarioId])
--   FK_Credito_Usuario4 : ([UsuarioModId]) -> [MAESTRO].[Usuario] ([UsuarioId])
