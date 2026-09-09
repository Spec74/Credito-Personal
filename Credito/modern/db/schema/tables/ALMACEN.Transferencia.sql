-- Tabla: [ALMACEN].[Transferencia]

CREATE TABLE [ALMACEN].[Transferencia] (
    [TransferenciaId] int IDENTITY NOT NULL,
    [AlmacenOrigenId] int NOT NULL,
    [AlmacenDestinoId] int NOT NULL,
    [UsuarioId] int NOT NULL,
    [Fecha] datetime NOT NULL,
    [Estado] char(3) NOT NULL
);

-- Indices
--   PRIMARY KEY PK_Transferencia (CLUSTERED): [TransferenciaId]

-- Claves foraneas
--   FK__Transfere__Almac__3552E9B6 : ([AlmacenOrigenId]) -> [ALMACEN].[Almacen] ([AlmacenId])
--   FK__Transfere__Almac__36470DEF : ([AlmacenDestinoId]) -> [ALMACEN].[Almacen] ([AlmacenId])
--   FK__Transfere__Usuar__373B3228 : ([UsuarioId]) -> [MAESTRO].[Usuario] ([UsuarioId])
