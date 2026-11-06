-- PLIN como marca activa (TablaId=13, ItemId=9).
-- 6–8 siguen OBSOLETO (sede disfrazada). Bóveda usa marcas activas, no el rango fijo 1–5.
-- Idempotente: seed + CREATE OR ALTER + BovedaCuenta en bóvedas abiertas.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ── 1) Catálogo ─────────────────────────────────────────────────────────── */
IF NOT EXISTS (
    SELECT 1
    FROM MAESTRO.ValorTabla
    WHERE TablaId = 13 AND ItemId = 9
)
BEGIN
    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
    VALUES (13, 9, N'PLIN', NULL, NULL);
END
ELSE
BEGIN
    UPDATE MAESTRO.ValorTabla
    SET Denominacion = N'PLIN',
        DesCorta = CASE
            WHEN ISNULL(DesCorta, N'') = N'OBSOLETO' THEN NULL
            ELSE DesCorta
        END
    WHERE TablaId = 13 AND ItemId = 9;
END
GO

/* ── 2) Cuenta Plin en bóvedas abiertas (principal y temporal) ───────────── */
INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
SELECT b.BovedaId, 9, CAST(0 AS decimal(16, 2))
FROM CREDITO.Boveda AS b
WHERE b.IndCierre = CAST(0 AS bit)
  AND NOT EXISTS (
      SELECT 1
      FROM CREDITO.BovedaCuenta AS bc
      WHERE bc.BovedaId = b.BovedaId
        AND bc.TipoPagoId = 9
  );
GO

/* ── 3) USP: resumen ─────────────────────────────────────────────────────── */
CREATE OR ALTER PROC [CREDITO].[usp_ResumenCuentaBoveda]
    @BovedaId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SaldoInicial DECIMAL(15, 2) = (
        SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId = @BovedaId
    );

    IF @SaldoInicial > 0
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaId
        )
        BEGIN
            INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
            VALUES (@BovedaId, 1, @SaldoInicial);
        END
    END

    ;WITH CUENTAS AS (
        SELECT
            MC.BovedaId,
            MC.TipoPagoId,
            SUM(IIF(MC.IndEntrada = 1, MC.Importe, -MC.Importe)) AS Importe
        FROM CREDITO.BovedaMov AS MC
        WHERE MC.BovedaId = @BovedaId
          AND MC.Estado = 1
        GROUP BY MC.BovedaId, MC.TipoPagoId
    )
    SELECT
        N'RESUMEN BOVEDA: '
        + STRING_AGG(
            vt.Denominacion
            + N' = '
            + CAST(ISNULL(BC.SaldoInicial, 0) + ISNULL(c.Importe, 0) AS VARCHAR(15)),
            N'  '
        ) AS Total
    FROM MAESTRO.ValorTabla AS vt
    LEFT JOIN CUENTAS AS c ON vt.ItemId = c.TipoPagoId
    LEFT JOIN CREDITO.BovedaCuenta AS BC
        ON BC.BovedaId = @BovedaId AND BC.TipoPagoId = vt.ItemId
    WHERE vt.TablaId = 13
      AND vt.ItemId > 0
      AND vt.ItemId NOT BETWEEN 6 AND 8
      AND ISNULL(vt.DesCorta, N'') <> N'OBSOLETO';
END
GO

/* ── 4) USP: cierre principal (arrastra marcas activas, incl. Plin) ──────── */
CREATE OR ALTER PROC [CREDITO].[usp_CerrarBoveda]
    @OficinaId INT,
    @UsuarioRegId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Principales INT = (
        SELECT COUNT(1)
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId AND IndTemporal = 0 AND IndCierre = 0
    );

    IF @Principales = 0
    BEGIN
        RAISERROR(N'No hay bóveda principal abierta en la oficina.', 16, 1);
        RETURN;
    END

    IF @Principales > 1
    BEGIN
        RAISERROR(N'Hay más de una bóveda principal abierta en la oficina. Corrija antes de cerrar.', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1
        FROM CREDITO.CajaDiario AS cd
        INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
        WHERE cd.IndCierre = 0 AND c.OficinaId = @OficinaId
    )
    BEGIN
        RAISERROR(N'EXISTEN CAJAS ABIERTAS.', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1
        FROM CREDITO.CajaDiario AS cd
        INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
        WHERE cd.IndCierre = 1 AND cd.TransBoveda = 0 AND c.OficinaId = @OficinaId
    )
    BEGIN
        RAISERROR(N'EXISTEN CAJAS CERRADAS NO ENVIADAS A BOVEDA. REVISE FORMULARIO SALDOS CAJA.', 16, 1);
        RETURN;
    END

    DECLARE @BovedaId INT = (
        SELECT BovedaId
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId AND IndTemporal = 0 AND IndCierre = 0
    );
    DECLARE @BovedaNuevoId INT;

    EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId;

    DECLARE @SaldoFinal DECIMAL(15, 2) = (
        SELECT SaldoFinal FROM CREDITO.Boveda WHERE BovedaId = @BovedaId
    );

    UPDATE CREDITO.Boveda
    SET IndCierre = 1, FechaFinOperacion = dbo.ufnFecha()
    WHERE BovedaId = @BovedaId;

    INSERT INTO CREDITO.Boveda (
        OficinaId, SaldoInicial, Entradas, Salidas, SaldoFinal,
        FechaIniOperacion, FechaFinOperacion, IndCierre, IndTemporal
    )
    VALUES (
        @OficinaId, @SaldoFinal, 0, 0, @SaldoFinal,
        dbo.ufnFecha(), NULL, 0, 0
    );

    SET @BovedaNuevoId = SCOPE_IDENTITY();

    ;WITH CUENTAS AS (
        SELECT
            MC.BovedaId,
            MC.TipoPagoId,
            SUM(IIF(MC.IndEntrada = 1, MC.Importe, -MC.Importe)) AS Importe
        FROM CREDITO.BovedaMov AS MC
        WHERE MC.BovedaId = @BovedaId
          AND MC.Estado = 1
        GROUP BY MC.BovedaId, MC.TipoPagoId
    )
    INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
    SELECT
        @BovedaNuevoId,
        vt.ItemId,
        ISNULL(BC.SaldoInicial, 0) + ISNULL(c.Importe, 0)
    FROM MAESTRO.ValorTabla AS vt
    LEFT JOIN CUENTAS AS c ON vt.ItemId = c.TipoPagoId
    LEFT JOIN CREDITO.BovedaCuenta AS BC
        ON BC.BovedaId = @BovedaId AND BC.TipoPagoId = vt.ItemId
    WHERE vt.TablaId = 13
      AND vt.ItemId > 0
      AND vt.ItemId NOT BETWEEN 6 AND 8
      AND ISNULL(vt.DesCorta, N'') <> N'OBSOLETO';

    DELETE FROM CREDITO.BovedaCuenta
    WHERE BovedaId = @BovedaNuevoId AND SaldoInicial = 0;
END
GO

/* ── 5) USP: cierre temporal ─────────────────────────────────────────────── */
CREATE OR ALTER PROC [CREDITO].[usp_CerrarBovedaTemporal]
    @OficinaId INT,
    @UsuarioRegId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @BovedaId INT;
    DECLARE @BovedaIdTemporal INT;
    DECLARE @ImporteBovedaTemporal DECIMAL(15, 2);
    DECLARE @RolEncargado INT;

    SELECT
        @BovedaIdTemporal = BovedaId,
        @ImporteBovedaTemporal = SaldoFinal
    FROM CREDITO.Boveda
    WHERE OficinaId = @OficinaId AND IndTemporal = 1 AND IndCierre = 0;

    SELECT @BovedaId = BovedaId
    FROM CREDITO.Boveda
    WHERE OficinaId = @OficinaId AND IndTemporal = 0 AND IndCierre = 0;

    IF @BovedaIdTemporal IS NULL
    BEGIN
        RAISERROR(N'No hay bóveda temporal abierta en la oficina.', 16, 1);
        RETURN;
    END

    IF @BovedaId IS NULL
    BEGIN
        RAISERROR(N'No hay bóveda principal abierta para devolver el saldo temporal.', 16, 1);
        RETURN;
    END

    EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaIdTemporal;

    ;WITH CUENTAS AS (
        SELECT
            MC.BovedaId,
            MC.TipoPagoId,
            SUM(IIF(MC.IndEntrada = 1, MC.Importe, -MC.Importe)) AS Importe
        FROM CREDITO.BovedaMov AS MC
        WHERE MC.BovedaId = @BovedaIdTemporal
          AND MC.Estado = 1
        GROUP BY MC.BovedaId, MC.TipoPagoId
    )
    INSERT INTO CREDITO.BovedaMov (
        BovedaId, CodOperacion, Glosa, TipoPagoId, Importe,
        IndEntrada, Estado, CajaDiarioId, UsuarioRegId, FechaReg
    )
    SELECT
        @BovedaId,
        N'TRE',
        N'CIERRE BOVEDA TEMPORAL',
        vt.ItemId,
        ISNULL(BC.SaldoInicial, 0) + ISNULL(c.Importe, 0),
        1,
        1,
        0,
        @UsuarioRegId,
        dbo.ufnFecha()
    FROM MAESTRO.ValorTabla AS vt
    LEFT JOIN CUENTAS AS c ON vt.ItemId = c.TipoPagoId
    LEFT JOIN CREDITO.BovedaCuenta AS BC
        ON BC.BovedaId = @BovedaIdTemporal AND BC.TipoPagoId = vt.ItemId
    WHERE vt.TablaId = 13
      AND vt.ItemId > 0
      AND vt.ItemId NOT BETWEEN 6 AND 8
      AND ISNULL(vt.DesCorta, N'') <> N'OBSOLETO'
      AND ISNULL(BC.SaldoInicial, 0) + ISNULL(c.Importe, 0) <> 0;

    UPDATE CREDITO.Boveda
    SET IndCierre = 1, FechaFinOperacion = dbo.ufnFecha()
    WHERE BovedaId = @BovedaIdTemporal;

    EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId;

    SELECT @RolEncargado = r.RolId
    FROM MAESTRO.Rol AS r
    WHERE r.Denominacion = N'ENCARGADO';

    IF @RolEncargado IS NOT NULL
    BEGIN
        DELETE FROM MAESTRO.UsuarioRol
        WHERE RolId = @RolEncargado AND OficinaId = @OficinaId;
    END
END
GO

/* ── 6) USP: transferencia entre bóvedas (marca activa) ──────────────────── */
CREATE OR ALTER PROC [CREDITO].[usp_TransferirBoveda](
    @BovedaInicioId INT,
    @BovedaDestinoId INT,
    @Glosa VARCHAR(MAX),
    @Monto DECIMAL(16, 2),
    @UsuarioRegId INT,
    @flagAceptar INT,
    @BovedaMovTempId INT = 0,
    @TipoPagoId SMALLINT = 1
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Estado BIT = 1;
    DECLARE @FechaReg DATETIME = dbo.ufnFecha();

    IF ISNULL(@TipoPagoId, 0) < 1
        SET @TipoPagoId = 1;

    IF @flagAceptar = 0
    BEGIN
        IF @BovedaInicioId IS NULL OR @BovedaDestinoId IS NULL
            OR @BovedaInicioId < 1 OR @BovedaDestinoId < 1
        BEGIN
            RAISERROR(N'Origen y destino de bóveda son obligatorios.', 16, 1);
            RETURN;
        END

        IF @BovedaInicioId = @BovedaDestinoId
        BEGIN
            RAISERROR(N'La bóveda de origen y destino no pueden ser la misma.', 16, 1);
            RETURN;
        END

        IF @Monto IS NULL OR @Monto <= 0
        BEGIN
            RAISERROR(N'El monto debe ser mayor a cero.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM MAESTRO.ValorTabla AS vt
            WHERE vt.TablaId = 13
              AND vt.ItemId = @TipoPagoId
              AND vt.ItemId > 0
              AND vt.ItemId NOT BETWEEN 6 AND 8
              AND ISNULL(vt.DesCorta, N'') <> N'OBSOLETO'
        )
        BEGIN
            RAISERROR(
                N'El medio debe ser una marca activa (Efectivo, Yape, Interbank, BCP, Nación, Plin). No use tipos de sede (6-8).',
                16,
                1
            );
            RETURN;
        END

        IF NOT EXISTS (
            SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = @BovedaInicioId AND IndCierre = 0
        )
        BEGIN
            RAISERROR(N'La bóveda de origen no está abierta.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (
            SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = @BovedaDestinoId AND IndCierre = 0
        )
        BEGIN
            RAISERROR(N'La bóveda de destino no está abierta.', 16, 1);
            RETURN;
        END

        DECLARE @HayCuentas BIT = CASE
            WHEN EXISTS (SELECT 1 FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaInicioId)
            THEN 1 ELSE 0
        END;
        DECLARE @SiBoveda DECIMAL(16, 2) = (
            SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId = @BovedaInicioId
        );
        DECLARE @SiMedio DECIMAL(16, 2) = ISNULL((
            SELECT SaldoInicial
            FROM CREDITO.BovedaCuenta
            WHERE BovedaId = @BovedaInicioId AND TipoPagoId = @TipoPagoId
        ), 0);
        IF @HayCuentas = 0 AND @TipoPagoId = 1
            SET @SiMedio = ISNULL(@SiBoveda, 0);

        DECLARE @NetoMedio DECIMAL(16, 2) = ISNULL((
            SELECT SUM(IIF(IndEntrada = 1, Importe, -Importe))
            FROM CREDITO.BovedaMov
            WHERE BovedaId = @BovedaInicioId
              AND Estado = 1
              AND TipoPagoId = @TipoPagoId
        ), 0);

        IF (@SiMedio + @NetoMedio) < @Monto
        BEGIN
            RAISERROR(N'Saldo insuficiente en el medio de origen.', 16, 1);
            RETURN;
        END

        INSERT INTO CREDITO.BovedaMov (
            BovedaId, CodOperacion, Glosa, Importe, IndEntrada,
            Estado, UsuarioRegId, FechaReg, TipoPagoId
        )
        VALUES (
            @BovedaInicioId, N'TRS', @Glosa, @Monto, 0,
            @Estado, @UsuarioRegId, @FechaReg, @TipoPagoId
        );
        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId;

        INSERT INTO CREDITO.BovedaMov (
            BovedaId, CodOperacion, Glosa, Importe, IndEntrada,
            Estado, UsuarioRegId, FechaReg, TipoPagoId
        )
        VALUES (
            @BovedaDestinoId, N'TRE', @Glosa, @Monto, 1,
            @Estado, @UsuarioRegId, @FechaReg, @TipoPagoId
        );
        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaDestinoId;
    END

    IF @flagAceptar = 2
    BEGIN
        DELETE FROM CREDITO.BovedaMov
        WHERE MovimientoBovedaId = (
            SELECT MovimientoBovedaIniId
            FROM CREDITO.BovedaMovTemp
            WHERE BovedaMovTempId = @BovedaMovTempId
        );

        SET @BovedaInicioId = (
            SELECT BovedaInicioId
            FROM CREDITO.BovedaMovTemp
            WHERE BovedaMovTempId = @BovedaMovTempId
        );

        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId;

        DELETE FROM CREDITO.BovedaMovTemp
        WHERE BovedaMovTempId = @BovedaMovTempId;
    END
END
GO

/* ── 7) Verificación rápida ──────────────────────────────────────────────── */
SELECT ItemId, Denominacion, DesCorta
FROM MAESTRO.ValorTabla
WHERE TablaId = 13
  AND ItemId IN (1, 2, 3, 4, 5, 6, 7, 8, 9)
ORDER BY ItemId;

SELECT COUNT(1) AS BovedasConPlin
FROM CREDITO.BovedaCuenta
WHERE TipoPagoId = 9;
GO
