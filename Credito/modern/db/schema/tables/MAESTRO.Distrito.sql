-- Tabla: [MAESTRO].[Distrito]

CREATE TABLE [MAESTRO].[Distrito] (
    [idDist] int NOT NULL DEFAULT ('0'),
    [Denominacion] varchar(50) NOT NULL DEFAULT (NULL),
    [idProv] int NOT NULL DEFAULT (NULL)
);

-- Indices
--   PRIMARY KEY PK__Distrito__DCADBD0B1B6A1BC8 (CLUSTERED): [idDist]

-- Claves foraneas
--   FK__Distrito__idProv__37FA4C37 : ([idProv]) -> [MAESTRO].[Provincia] ([idProv])
