-- Tabla: [MAESTRO].[Menu]

CREATE TABLE [MAESTRO].[Menu] (
    [MenuId] int IDENTITY NOT NULL,
    [Denominacion] varchar(255) NULL,
    [Modulo] varchar(255) NULL,
    [Url] varchar(255) NULL,
    [Icono] varchar(255) NULL,
    [IndPadre] bit NULL,
    [Orden] decimal(3,1) NULL,
    [Referencia] decimal(3,1) NULL
);

-- Indices
--   PRIMARY KEY PK__Menu__C99ED2306522C3C0 (CLUSTERED): [MenuId]
