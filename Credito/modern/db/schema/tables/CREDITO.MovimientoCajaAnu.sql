-- Tabla: [CREDITO].[MovimientoCajaAnu]

CREATE TABLE [CREDITO].[MovimientoCajaAnu] (
    [MovimientoCajaAnuId] int IDENTITY NOT NULL,
    [MovimientoCajaId] int NULL,
    [Observacion] varchar(max) NULL,
    [UsuarioRegId] int NULL,
    [FechaReg] datetime NULL
);

-- Indices
--   PRIMARY KEY PK__Movimien__E2AB2E7E4FBD9286 (CLUSTERED): [MovimientoCajaAnuId]

-- Claves foraneas
--   FK__Movimient__Movim__51A5DAF8 : ([MovimientoCajaId]) -> [CREDITO].[MovimientoCaja] ([MovimientoCajaId])
