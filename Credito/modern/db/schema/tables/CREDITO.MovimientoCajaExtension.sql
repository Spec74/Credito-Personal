-- Tabla: [CREDITO].[MovimientoCajaExtension]

CREATE TABLE [CREDITO].[MovimientoCajaExtension] (
    [MovimientoCajaId] int NOT NULL,
    [FechaTransferencia] varchar(16) NULL,
    [IndTransferenciaVerificada] bit NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__Movimien__266F555FBB8240C2 (CLUSTERED): [MovimientoCajaId]

-- Claves foraneas
--   FK__Movimient__Movim__31A25463 : ([MovimientoCajaId]) -> [CREDITO].[MovimientoCaja] ([MovimientoCajaId])
