-- Tabla: [CREDITO].[CuentaxCobrar]

CREATE TABLE [CREDITO].[CuentaxCobrar] (
    [CuentaxCobrarId] int IDENTITY NOT NULL,
    [Operacion] char(3) NOT NULL,
    [Monto] decimal(16,2) NOT NULL DEFAULT ((0)),
    [Estado] char(3) NOT NULL,
    [MovimientoCajaId] int NULL,
    [CreditoId] int NULL
);

-- Indices
--   PRIMARY KEY PK__Cuentasx__D5EF24B3377BF4A1 (CLUSTERED): [CuentaxCobrarId]

-- Claves foraneas
--   FK__CuentasxC__Movim__3A58614C : ([MovimientoCajaId]) -> [CREDITO].[MovimientoCaja] ([MovimientoCajaId])
--   FK__CuentaxCo__Credi__3D69D821 : ([CreditoId]) -> [CREDITO].[Credito] ([CreditoId])
