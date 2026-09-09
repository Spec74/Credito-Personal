-- Tabla: [CREDITO].[CajaChicaDiario]

CREATE TABLE [CREDITO].[CajaChicaDiario] (
    [Id] int IDENTITY NOT NULL,
    [UsuarioId] int NOT NULL,
    [SaldoInicial] decimal(15,2) NOT NULL DEFAULT ((0)),
    [Entradas] decimal(15,2) NOT NULL DEFAULT ((0)),
    [Salidas] decimal(15,2) NOT NULL DEFAULT ((0)),
    [SaldoFinal] decimal(15,2) NOT NULL DEFAULT ((0)),
    [FechaIniOperacion] datetime NOT NULL,
    [FechaFinOperacion] datetime NULL,
    [IndCierre] bit NOT NULL DEFAULT ((0)),
    [TransBoveda] bit NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__CajaChic__3214EC07A8707266 (CLUSTERED): [Id]

-- Claves foraneas
--   FK__CajaChica__Usuar__473C8FC7 : ([UsuarioId]) -> [MAESTRO].[Usuario] ([UsuarioId])
