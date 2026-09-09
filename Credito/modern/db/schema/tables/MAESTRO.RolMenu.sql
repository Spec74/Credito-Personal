-- Tabla: [MAESTRO].[RolMenu]

CREATE TABLE [MAESTRO].[RolMenu] (
    [RolMenuId] int IDENTITY NOT NULL,
    [RolId] int NULL,
    [MenuId] int NULL
);

-- Indices
--   PRIMARY KEY PK__RolMenu__8339C1FE68F354A4 (CLUSTERED): [RolMenuId]

-- Claves foraneas
--   FK__RolMenu__MenuId__6BCFC14F : ([MenuId]) -> [MAESTRO].[Menu] ([MenuId])
--   FK__RolMenu__RolId__6ADB9D16 : ([RolId]) -> [MAESTRO].[Rol] ([RolId])
