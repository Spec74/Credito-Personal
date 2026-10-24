-- EXEC CREDITO.usp_CerrarCajasDiarios @UsuarioCierreId, @OficinaId
-- Destino: bóveda de @OficinaId (multisucursal). Importe a 2 decimales.

CREATE OR ALTER PROC [CREDITO].[usp_CerrarCajasDiarios]
    @UsuarioCierreId INT,
    @OficinaId INT = 1,
    @Sobrante DECIMAL(15, 2) = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @FechaTexto VARCHAR(20) = 'CIERRE ' + CONVERT(varchar, @Fecha, 103) + ' ';

    DECLARE @Principales INT = (
        SELECT COUNT(1)
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = 0
          AND IndTemporal = 0
    );
    IF @Principales > 1
    BEGIN
        RAISERROR(N'Hay más de una bóveda principal abierta en la oficina.', 16, 1);
        RETURN;
    END

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
        ROUND(CU.Importe, 2),
        1,
        1,
        @UsuarioCierreId,
        @Fecha
    FROM CUENTAS CU
    WHERE ROUND(CU.Importe, 2) <> 0
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
