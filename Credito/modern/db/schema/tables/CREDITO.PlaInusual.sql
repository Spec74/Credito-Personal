-- Tabla: [CREDITO].[PlaInusual]

CREATE TABLE [CREDITO].[PlaInusual] (
    [PlaInusualId] int IDENTITY NOT NULL,
    [OficinaId] int NOT NULL,
    [PersonaId] int NOT NULL,
    [CreditoId] int NULL,
    [Calificacion] varchar(12) NOT NULL,
    [Motivo] varchar(500) NOT NULL,
    [UsuarioRegId] int NOT NULL,
    [FechaReg] datetime NOT NULL
);

-- Indices
--   PRIMARY KEY PK_PlaInusual (CLUSTERED): [PlaInusualId]
--   INDEX IX_PlaInusual_Oficina_Fecha (NONCLUSTERED): [OficinaId], [FechaReg]
