-- Tabla: [MAESTRO].[ValorTabla]

CREATE TABLE [MAESTRO].[ValorTabla] (
    [TablaId] int NOT NULL,
    [ItemId] int NOT NULL,
    [Denominacion] varchar(70) NULL,
    [DesCorta] varchar(30) NULL,
    [Valor] varchar(100) NULL
);

-- Indices
--   PRIMARY KEY PK_VALORTABLA (CLUSTERED): [TablaId], [ItemId]
