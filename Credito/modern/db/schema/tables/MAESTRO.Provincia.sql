-- Tabla: [MAESTRO].[Provincia]

CREATE TABLE [MAESTRO].[Provincia] (
    [idProv] int NOT NULL DEFAULT ('0'),
    [Denominacion] varchar(50) NOT NULL DEFAULT (NULL),
    [idDepa] int NOT NULL DEFAULT (NULL)
);

-- Indices
--   PRIMARY KEY PK__Provinci__B41BB0D891EB6DA6 (CLUSTERED): [idProv]

-- Claves foraneas
--   FK__Provincia__idDep__38EE7070 : ([idDepa]) -> [MAESTRO].[Departamento] ([idDepa])
