-- Tabla: [CREDITO].[Tarea]

CREATE TABLE [CREDITO].[Tarea] (
    [TareaId] int IDENTITY NOT NULL,
    [CreditoId] int NOT NULL,
    [FechaCreacion] datetime NOT NULL,
    [FechaCompletada] datetime NULL,
    [Estado] char(3) NOT NULL DEFAULT ('P'),
    [UsuarioCreadorId] int NOT NULL
);

-- Indices
--   PRIMARY KEY PK__Tarea__5CD839916C1DA624 (CLUSTERED): [TareaId]

-- Claves foraneas
--   FK__Tarea__CreditoId__78F3E6EC : ([CreditoId]) -> [CREDITO].[Credito] ([CreditoId])
