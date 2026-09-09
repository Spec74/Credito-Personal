-- Tabla: [MAESTRO].[UsuarioRol]

CREATE TABLE [MAESTRO].[UsuarioRol] (
    [UsuarioRolId] int IDENTITY NOT NULL,
    [UsuarioId] int NULL,
    [RolId] int NULL,
    [OficinaId] int NULL
);

-- Indices
--   PRIMARY KEY PK__UsuarioR__C869CDCA6EAC2DFA (CLUSTERED): [UsuarioRolId]

-- Claves foraneas
--   FK__UsuarioRo__Ofici__764D4FC2 : ([OficinaId]) -> [MAESTRO].[Oficina] ([OficinaId])
--   FK__UsuarioRo__RolId__71889AA5 : ([RolId]) -> [MAESTRO].[Rol] ([RolId])
--   FK__UsuarioRo__Usuar__7094766C : ([UsuarioId]) -> [MAESTRO].[Usuario] ([UsuarioId])
