-- Tabla: [VENTAS].[OrdenVenta]

CREATE TABLE [VENTAS].[OrdenVenta] (
    [OrdenVentaId] int IDENTITY NOT NULL,
    [OficinaId] int NOT NULL,
    [Subtotal] decimal(16,2) NOT NULL DEFAULT ((0)),
    [TotalImpuesto] decimal(16,2) NOT NULL DEFAULT ((0)),
    [TotalNeto] decimal(16,2) NOT NULL DEFAULT ((0)),
    [TotalDescuento] decimal(16,2) NOT NULL DEFAULT ((0)),
    [Estado] char(3) NOT NULL,
    [UsuarioRegId] int NOT NULL,
    [FechaReg] datetime NOT NULL,
    [UsuarioModId] int NULL,
    [FechaMod] datetime NULL,
    [PersonaId] int NOT NULL,
    [MovimientoAlmacenId] int NULL,
    [TipoVenta] char(3) NOT NULL DEFAULT ('CON')
);

-- Indices
--   PRIMARY KEY PK__OrdenVen__16E7FA0661BC4730 (CLUSTERED): [OrdenVentaId]

-- Claves foraneas
--   FK_ORDENVENTA_OficinaId : ([OficinaId]) -> [MAESTRO].[Oficina] ([OficinaId])
--   FK_OrdenVenta_Persona : ([PersonaId]) -> [MAESTRO].[Persona] ([PersonaId])
--   FK_OrdenVenta_Usuario : ([UsuarioRegId]) -> [MAESTRO].[Usuario] ([UsuarioId])
--   FK_OrdenVenta_Usuario1 : ([UsuarioModId]) -> [MAESTRO].[Usuario] ([UsuarioId])
