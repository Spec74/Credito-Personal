-- Tabla: [CREDITO].[Subtarea]

CREATE TABLE [CREDITO].[Subtarea] (
    [SubtareaId] int IDENTITY NOT NULL,
    [TareaId] int NOT NULL,
    [Titulo] nvarchar(500) NOT NULL,
    [Completada] bit NOT NULL DEFAULT ((0)),
    [FechaCompletada] datetime NULL
);

-- Indices
--   PRIMARY KEY PK__Subtarea__B98E0AEE61544400 (CLUSTERED): [SubtareaId]

-- Claves foraneas
--   FK__Subtarea__TareaI__7DB89C09 : ([TareaId]) -> [CREDITO].[Tarea] ([TareaId])
