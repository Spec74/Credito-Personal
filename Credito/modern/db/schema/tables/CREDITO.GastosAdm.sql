-- Tabla: [CREDITO].[GastosAdm]

CREATE TABLE [CREDITO].[GastosAdm] (
    [GastosAdmId] int IDENTITY NOT NULL,
    [Denominacion] varchar(50) NOT NULL,
    [MontoMinimo] decimal(16,2) NOT NULL,
    [MontoMaximo] decimal(16,2) NOT NULL,
    [IndPorcentaje] bit NOT NULL,
    [Valor] decimal(16,2) NOT NULL,
    [Estado] bit NOT NULL,
    [Ind] bit NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__GastosAd__B499EA5B2962DF74 (CLUSTERED): [GastosAdmId]
