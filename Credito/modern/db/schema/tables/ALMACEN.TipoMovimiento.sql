-- Tabla: [ALMACEN].[TipoMovimiento]

CREATE TABLE [ALMACEN].[TipoMovimiento] (
    [TipoMovimientoId] int IDENTITY NOT NULL,
    [Denominacion] varchar(70) NULL,
    [Descripcion] varchar(250) NULL,
    [IndEntrada] bit NOT NULL,
    [IndTransferencia] bit NULL,
    [IndDevolucion] bit NULL,
    [Estado] bit NOT NULL
);

-- Indices
--   PRIMARY KEY PK__TipoMovi__097C736171C061AE (CLUSTERED): [TipoMovimientoId]
