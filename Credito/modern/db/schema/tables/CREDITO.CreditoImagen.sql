-- Tabla: [CREDITO].[CreditoImagen]

CREATE TABLE [CREDITO].[CreditoImagen] (
    [Id] int IDENTITY NOT NULL,
    [CreditoId] int NOT NULL,
    [Imagen] varchar(50) NOT NULL
);

-- Indices
--   PRIMARY KEY PK__CreditoI__3214EC074098E894 (CLUSTERED): [Id]

-- Claves foraneas
--   FK__CreditoIm__Credi__1BE81D6E : ([CreditoId]) -> [CREDITO].[Credito] ([CreditoId])
