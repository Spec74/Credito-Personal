-- Tabla: [CREDITO].[BovedaTemporalResponsable]
-- Quién operó la temporal y si se le otorgó ENCARGADO al crearla.

CREATE TABLE [CREDITO].[BovedaTemporalResponsable] (
    [BovedaId] int NOT NULL,
    [OficinaId] int NOT NULL,
    [UsuarioId] int NOT NULL,
    [OtorgoEncargado] bit NOT NULL DEFAULT ((0)),
    [FechaReg] datetime NOT NULL DEFAULT (dbo.ufnFecha())
);

-- Indices
--   PRIMARY KEY PK_BovedaTemporalResponsable (CLUSTERED): [BovedaId]
