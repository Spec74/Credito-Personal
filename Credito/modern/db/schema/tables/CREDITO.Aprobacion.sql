-- Tabla: [CREDITO].[Aprobacion]

CREATE TABLE [CREDITO].[Aprobacion] (
    [CreditoId] int NOT NULL,
    [Nivel] int NOT NULL,
    [UsuarioId] int NULL,
    [Fecha] datetime NULL
);

-- Indices
--   PRIMARY KEY PK__CreditoE__4FE406DDCF29B870 (CLUSTERED): [CreditoId], [Nivel]

-- Claves foraneas
--   FK__CreditoEx__Credi__5AFA3B08 : ([CreditoId]) -> [CREDITO].[Credito] ([CreditoId])
--   FK__CreditoEx__Usuar__5BEE5F41 : ([UsuarioId]) -> [MAESTRO].[Usuario] ([UsuarioId])
