-- Multisucursal: usp_CerrarCajasDiarios
--
-- Producción (2026-10-16) tenía:
--   1) Destino FORZADO a bóveda principal de OficinaId = 1 (central / dashboard).
--   2) CTE MOVS_BASE: SaldoInicial como efectivo (TipoPagoId=1) UNION ALL movimientos
--      del día, consolidado por caja/tipo — correcto para no perder efectivo físico.
--   3) Origen de cajas ya filtrado por @OficinaId.
--
-- Cambio para autonomía de sucursales (Pichari, Huanta, Andahuaylas):
--   El destino pasa a ser la bóveda ABIERTA de @OficinaId (principal; si hay temporal
--   abierta de esa oficina, se usa la temporal — mismo criterio legacy del schema).
--   Se conserva la reestructuración de efectivo (MOVS_BASE).
--
-- Ver: docs/migration/PLAN-MULTISUCURSAL-PRENDARIO.md (B-01)

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC [CREDITO].[usp_CerrarCajasDiarios]
    @UsuarioCierreId INT,
    @OficinaId INT = 1,
    @Sobrante DECIMAL(15, 2) = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @FechaTexto VARCHAR(20) = 'CIERRE ' + CONVERT(varchar, @Fecha, 103) + ' ';

    -- Destino: bóveda de LA MISMA oficina que cierra (no forzar central / OficinaId=1)
    DECLARE @BovedaId INT = (
        SELECT BovedaId
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = 0
          AND IndTemporal = 0
    );

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = 0
          AND IndTemporal = 1
    )
        SET @BovedaId = (
            SELECT BovedaId
            FROM CREDITO.Boveda
            WHERE OficinaId = @OficinaId
              AND IndCierre = 0
              AND IndTemporal = 1
        );

    IF @BovedaId IS NULL
    BEGIN
        RAISERROR(
            N'No se pueden cerrar las cajas: no existe una bóveda activa abierta en la oficina.',
            16,
            1
        );
        RETURN;
    END;

    -- Saldo inicial físico (efectivo) + movimientos del día (bancos / apps), por oficina
    ;WITH MOVS_BASE AS (
        SELECT
            C.Denominacion AS Caja,
            CD.CajaDiarioId,
            1 AS TipoPagoId,
            CD.SaldoInicial AS Importe
        FROM CREDITO.CajaDiario CD
        INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
        WHERE CD.IndCierre = 1
          AND CD.TransBoveda = 0
          AND C.OficinaId = @OficinaId

        UNION ALL

        SELECT
            C.Denominacion AS Caja,
            CD.CajaDiarioId,
            MC.TipoPagoId,
            SUM(IIF(MC.IndEntrada = 1, MC.ImportePago, -MC.ImportePago)) AS Importe
        FROM CREDITO.MovimientoCaja MC
        INNER JOIN CREDITO.CajaDiario CD ON CD.CajaDiarioId = MC.CajaDiarioId
        INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
        WHERE CD.IndCierre = 1
          AND CD.TransBoveda = 0
          AND C.OficinaId = @OficinaId
          AND MC.Estado = 1
        GROUP BY C.Denominacion, CD.CajaDiarioId, MC.TipoPagoId
    ),
    CUENTAS AS (
        SELECT
            Caja,
            CajaDiarioId,
            TipoPagoId,
            SUM(Importe) AS Importe
        FROM MOVS_BASE
        GROUP BY Caja, CajaDiarioId, TipoPagoId
    )
    INSERT INTO CREDITO.BovedaMov (
        BovedaId,
        CodOperacion,
        Glosa,
        CajaDiarioId,
        TipoPagoId,
        Importe,
        IndEntrada,
        Estado,
        UsuarioRegId,
        FechaReg
    )
    SELECT
        @BovedaId,
        'TRE',
        @FechaTexto + CU.Caja,
        CU.CajaDiarioId,
        CU.TipoPagoId,
        ROUND(CU.Importe, 1),
        1,
        1,
        @UsuarioCierreId,
        @Fecha
    FROM CUENTAS CU
    ORDER BY CU.CajaDiarioId, CU.TipoPagoId;

    IF @Sobrante > 0
        INSERT INTO CREDITO.BovedaMov (
            BovedaId,
            CodOperacion,
            Glosa,
            CajaDiarioId,
            TipoPagoId,
            Importe,
            IndEntrada,
            Estado,
            UsuarioRegId,
            FechaReg
        )
        VALUES (
            @BovedaId,
            'TRE',
            @FechaTexto + 'SOBRANTE',
            0,
            1,
            @Sobrante,
            1,
            1,
            @UsuarioCierreId,
            @Fecha
        );

    UPDATE CD
    SET CD.TransBoveda = 1
    FROM CREDITO.CajaDiario CD
    INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
    WHERE CD.IndCierre = 1
      AND CD.TransBoveda = 0
      AND C.OficinaId = @OficinaId;

    EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId;
END
GO
