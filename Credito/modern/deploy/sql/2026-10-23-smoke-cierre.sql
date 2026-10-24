-- Evidencia de cierre (solo lectura). Pegar en Azure Query Editor.
-- Esperado: una fila, todos los bits = 1, IxCalientes >= 10.

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
