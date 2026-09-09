-- Tabla: [CREDITO].[CajaDiario]

CREATE TABLE [CREDITO].[CajaDiario] (
    [CajaDiarioId] int IDENTITY NOT NULL,
    [CajaId] int NOT NULL,
    [UsuarioAsignadoId] int NOT NULL,
    [SaldoInicial] decimal(16,2) NOT NULL,
    [Entradas] decimal(16,2) NOT NULL DEFAULT ((0)),
    [Salidas] decimal(16,2) NOT NULL DEFAULT ((0)),
    [SaldoFinal] decimal(16,2) NOT NULL,
    [FechaIniOperacion] datetime NOT NULL,
    [FechaFinOperacion] datetime NULL,
    [IndCierre] bit NOT NULL DEFAULT ((0)),
    [TransBoveda] bit NOT NULL DEFAULT ((0)),
    [MontoPorCobrar] decimal(16,2) NOT NULL DEFAULT ((0)),
    [MontoCobrado] decimal(16,2) NOT NULL DEFAULT ((0)),
    [SaldoCartera] decimal(16,2) NOT NULL DEFAULT ((0)),
    [NroClientesSaldoCartera] int NOT NULL DEFAULT ((0)),
    [SaldoMoraCartera] decimal(16,2) NOT NULL DEFAULT ((0)),
    [NroClientesSaldoMoraCartera] int NOT NULL DEFAULT ((0)),
    [NroClientesNuevos] int NOT NULL DEFAULT ((0))
);

-- Indices
--   PRIMARY KEY PK__CajaDiar__A7FC1B240C0786B7 (CLUSTERED): [CajaDiarioId]

-- Claves foraneas
--   FK__CajaDiari__CajaI__0EE3F362 : ([CajaId]) -> [CREDITO].[Caja] ([CajaId])
--   FK__CajaDiari__Usuar__0DEFCF29 : ([UsuarioAsignadoId]) -> [MAESTRO].[Usuario] ([UsuarioId])
