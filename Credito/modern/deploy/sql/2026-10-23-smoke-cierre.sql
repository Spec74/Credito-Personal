-- Evidencia de cierre (solo lectura). Pegar en Azure Query Editor.
-- Núcleo (23-oct Azure): bits 1, PlaInusual=1, IxCalientes=11, FkPlaTrusted=1.
-- Segunda grilla: PlaParametro=1; TcSbs=1 solo si el oficial cargó el TC SBS.

SET NOCOUNT ON;

SELECT
    CAST(CASE WHEN OBJECT_ID(N'CREDITO.usp_SolicitudCredito_Del', N'P') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS UspSolicitudDel,
    CAST(CASE WHEN COL_LENGTH(N'CREDITO.Credito', N'EsPrendario') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS EsPrendario,
    CAST(CASE WHEN OBJECT_ID(N'CREDITO.Prenda', N'U') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS Prenda,
    CAST(CASE WHEN OBJECT_ID(N'CREDITO.usp_CreditoMora_Registrar', N'P') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS MoraRegistrar,
    CAST((
        SELECT uses_quoted_identifier
        FROM sys.sql_modules
        WHERE object_id = OBJECT_ID(N'CREDITO.usp_Credito_Ins')
    ) AS bit) AS CreditoInsQuotedOn,
    CAST(CASE WHEN (
        SELECT CHARACTER_MAXIMUM_LENGTH
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = N'MAESTRO'
          AND TABLE_NAME = N'Usuario'
          AND COLUMN_NAME = N'ClaveUsuario'
    ) >= 256 THEN 1 ELSE 0 END AS bit) AS ClaveUsuario256,
    CAST(CASE WHEN OBJECT_ID(N'CREDITO.PlaInusual', N'U') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS PlaInusual,
    (
        SELECT COUNT(*)
        FROM sys.indexes
        WHERE name IN (
            N'IX_Cargo_CreditoId',
            N'IX_Tarea_CreditoId',
            N'IX_CreditoImagen_CreditoId',
            N'IX_CentralRiesgo_CreditoId',
            N'IX_CreditoMora_CreditoId',
            N'IX_CuentaxCobrar_CreditoId',
            N'IX_PlaInusual_CreditoId',
            N'IX_CajaDiario_Caja_IndCierre',
            N'IX_Caja_OficinaId',
            N'IX_BovedaMov_BovedaId',
            N'IX_BovedaMov_CajaDiarioId'
        )
    ) AS IxCalientes,
    (
        SELECT COUNT(*)
        FROM sys.foreign_keys
        WHERE name = N'FK_PlaInusual_Credito' AND is_not_trusted = 0
    ) AS FkPlaTrusted;
GO

-- PLA operativo: fila legal + TC SBS (oficial; no se inventa).
IF OBJECT_ID(N'CREDITO.PlaParametro', N'U') IS NULL
    SELECT CAST(0 AS bit) AS PlaParametro, CAST(0 AS bit) AS TcSbs;
ELSE
    SELECT
        CAST(CASE WHEN EXISTS (
            SELECT 1 FROM CREDITO.PlaParametro WHERE PlaParametroId = 1
        ) THEN 1 ELSE 0 END AS bit) AS PlaParametro,
        CAST(CASE WHEN EXISTS (
            SELECT 1
            FROM CREDITO.PlaParametro
            WHERE PlaParametroId = 1
              AND TcVentaMesAnterior IS NOT NULL
              AND TcVentaMesAnterior > 0
        ) THEN 1 ELSE 0 END AS bit) AS TcSbs;
GO

-- Qué falta (0 filas = alineado con local).
SELECT v.objeto
FROM (VALUES
    (N'CREDITO.PlaInusual', CASE WHEN OBJECT_ID(N'CREDITO.PlaInusual', N'U') IS NULL THEN 1 ELSE 0 END),
    (N'CREDITO.PlaParametro', CASE WHEN OBJECT_ID(N'CREDITO.PlaParametro', N'U') IS NULL THEN 1 ELSE 0 END),
    (N'IX_PlaInusual_CreditoId', CASE WHEN NOT EXISTS (
        SELECT 1 FROM sys.indexes WHERE name = N'IX_PlaInusual_CreditoId') THEN 1 ELSE 0 END),
    (N'FK_PlaInusual_Credito', CASE WHEN NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PlaInusual_Credito') THEN 1 ELSE 0 END)
) AS v(objeto, falta)
WHERE v.falta = 1;
GO

-- Consulta / login. Azure 23-oct: 8.
SELECT COUNT(*) AS IxConsulta
FROM sys.indexes
WHERE name IN (
    N'IX_Credito_PersonaId',
    N'IX_CreditoCondonacion_CreditoId',
    N'IX_Boveda_OficinaId',
    N'IX_BovedaCuenta_BovedaId',
    N'IX_Subtarea_TareaId',
    N'IX_Cliente_PersonaId',
    N'IX_UsuarioOficina_Usuario_Oficina',
    N'IX_UsuarioRol_Usuario_Oficina'
);
GO

-- Cobro / anulación. Azure 23-oct: 3.
SELECT COUNT(*) AS IxCobro
FROM sys.indexes
WHERE name IN (
    N'IX_CuentaxCobrar_MovimientoCajaId',
    N'IX_MovimientoCajaAnu_MovimientoCajaId',
    N'IX_CajaDiario_UsuarioAsignadoId'
);
GO

-- PLA desembolsos (tras 2026-10-23-indices-pla-desembolso.sql). Esperado: 1.
SELECT COUNT(*) AS IxPlaDesembolso
FROM sys.indexes
WHERE name = N'IX_Credito_Oficina_FechaDesembolso';
GO
