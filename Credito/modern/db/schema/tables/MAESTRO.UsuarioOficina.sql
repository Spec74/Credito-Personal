-- Tabla: [MAESTRO].[UsuarioOficina]

CREATE TABLE [MAESTRO].[UsuarioOficina] (
    [UsuarioOficinaId] int IDENTITY NOT NULL,
    [UsuarioId] int NOT NULL,
    [OficinaId] int NOT NULL,
    [Estado] bit NOT NULL
);

-- Indices
--   PRIMARY KEY PK__UsuarioO__EF670E7473A5ED41 (CLUSTERED): [UsuarioOficinaId]

-- Claves foraneas
--   FK__UsuarioOf__Ofici__768259EC : ([OficinaId]) -> [MAESTRO].[Oficina] ([OficinaId])
--   FK__UsuarioOf__Usuar__758E35B3 : ([UsuarioId]) -> [MAESTRO].[Usuario] ([UsuarioId])
