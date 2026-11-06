-- Yape operativo en dos billeteras que suman al Yape total:
--   ItemId 10 = YAPE GERENTE (postable)
--   ItemId 11 = YAPE RENE   (postable)
--   ItemId  2 = YAPE        (ROLLUP / padre; no cobro ni fondeo nuevo)
-- 6–8 siguen OBSOLETO. Plin=9 sin cambios.
-- Migra saldo neto abierto de TipoPagoId=2 → 10 (Gerente) para no perder céntimos.
-- Idempotente.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ── 1) Catálogo ─────────────────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM MAESTRO.ValorTabla WHERE TablaId = 13 AND ItemId = 10)
    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
    VALUES (13, 10, N'YAPE GERENTE', NULL, N'2');
ELSE
    UPDATE MAESTRO.ValorTabla
    SET Denominacion = N'YAPE GERENTE',
        Valor = N'2',
        DesCorta = CASE WHEN ISNULL(DesCorta, N'') = N'OBSOLETO' THEN NULL ELSE DesCorta END
    WHERE TablaId = 13 AND ItemId = 10;

IF NOT EXISTS (SELECT 1 FROM MAESTRO.ValorTabla WHERE TablaId = 13 AND ItemId = 11)
    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
    VALUES (13, 11, N'YAPE RENE', NULL, N'2');
ELSE
    UPDATE MAESTRO.ValorTabla
    SET Denominacion = N'YAPE RENE',
        Valor = N'2',
        DesCorta = CASE WHEN ISNULL(DesCorta, N'') = N'OBSOLETO' THEN NULL ELSE DesCorta END
    WHERE TablaId = 13 AND ItemId = 11;

-- Padre Yape: rollup (histórico + total); no postable.
UPDATE MAESTRO.ValorTabla
SET Denominacion = N'YAPE',
    DesCorta = N'ROLLUP',
    Valor = NULL
WHERE TablaId = 13 AND ItemId = 2;
GO

/* ── 2) Cuentas 10/11 en bóvedas abiertas ────────────────────────────────── */
INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
SELECT b.BovedaId, v.ItemId, CAST(0 AS decimal(16, 2))
FROM CREDITO.Boveda AS b
CROSS JOIN (VALUES (10), (11)) AS v(ItemId)
WHERE b.IndCierre = CAST(0 AS bit)
  AND NOT EXISTS (
      SELECT 1
      FROM CREDITO.BovedaCuenta AS bc
      WHERE bc.BovedaId = b.BovedaId AND bc.TipoPagoId = v.ItemId
  );
GO

/* ── 3) Migrar saldo neto Yape (2) → Yape Gerente (10) en bóvedas abiertas ─ */
BEGIN TRAN;

DECLARE @BovedaId INT;
DECLARE @Neto DECIMAL(16, 2);
DECLARE @UsuarioRegId INT = (
    SELECT TOP (1) UsuarioId FROM MAESTRO.Usuario WHERE Estado = 1 ORDER BY UsuarioId
);

DECLARE c CURSOR LOCAL FAST_FORWARD FOR
SELECT b.BovedaId
FROM CREDITO.Boveda AS b
WHERE b.IndCierre = CAST(0 AS bit);

OPEN c;
FETCH NEXT FROM c INTO @BovedaId;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @Neto = CAST(
        ISNULL((
            SELECT SaldoInicial
            FROM CREDITO.BovedaCuenta
            WHERE BovedaId = @BovedaId AND TipoPagoId = 2
        ), 0)
        + ISNULL((
            SELECT SUM(IIF(IndEntrada = 1, Importe, -Importe))
            FROM CREDITO.BovedaMov
            WHERE BovedaId = @BovedaId AND Estado = 1 AND TipoPagoId = 2
        ), 0)
        AS decimal(16, 2)
    );

    -- Evitar re-migrar: si ya hay un asiento de migración reciente con esa glosa y neto≈0, skip.
    IF @Neto <> 0
       AND NOT EXISTS (
           SELECT 1
           FROM CREDITO.BovedaMov
           WHERE BovedaId = @BovedaId
             AND Estado = 1
             AND CodOperacion = N'TRS'
             AND Glosa LIKE N'MIGRACION YAPE→GERENTE%'
             AND TipoPagoId = 2
             AND ABS(Importe - ABS(@Neto)) < 0.005
       )
    BEGIN
        IF @Neto > 0
        BEGIN
            INSERT INTO CREDITO.BovedaMov (
                BovedaId, CodOperacion, Glosa, Importe, IndEntrada,
                Estado, UsuarioRegId, FechaReg, TipoPagoId
            )
            VALUES (
                @BovedaId, N'TRS', N'MIGRACION YAPE→GERENTE (roll-up)',
                @Neto, 0, 1, @UsuarioRegId, dbo.ufnFecha(), 2
            );

            INSERT INTO CREDITO.BovedaMov (
                BovedaId, CodOperacion, Glosa, Importe, IndEntrada,
                Estado, UsuarioRegId, FechaReg, TipoPagoId
            )
            VALUES (
                @BovedaId, N'TRE', N'MIGRACION YAPE→GERENTE (roll-up)',
                @Neto, 1, 1, @UsuarioRegId, dbo.ufnFecha(), 10
            );
        END
        ELSE
        BEGIN
            -- Neto negativo en Yape legado: sale de Gerente / entra a 2 para cerrar.
            DECLARE @Abs DECIMAL(16, 2) = ABS(@Neto);
            INSERT INTO CREDITO.BovedaMov (
                BovedaId, CodOperacion, Glosa, Importe, IndEntrada,
                Estado, UsuarioRegId, FechaReg, TipoPagoId
            )
            VALUES (
                @BovedaId, N'TRS', N'MIGRACION YAPE→GERENTE (roll-up)',
                @Abs, 0, 1, @UsuarioRegId, dbo.ufnFecha(), 10
            );

            INSERT INTO CREDITO.BovedaMov (
                BovedaId, CodOperacion, Glosa, Importe, IndEntrada,
                Estado, UsuarioRegId, FechaReg, TipoPagoId
            )
            VALUES (
                @BovedaId, N'TRE', N'MIGRACION YAPE→GERENTE (roll-up)',
                @Abs, 1, 1, @UsuarioRegId, dbo.ufnFecha(), 2
            );
        END

        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId;
    END

    FETCH NEXT FROM c INTO @BovedaId;
END
CLOSE c;
DEALLOCATE c;

COMMIT TRAN;
GO

/* ── 4) TransferirBoveda: marcas postables (no ROLLUP / no 6–8) ──────────── */
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
              AND ISNULL(vt.DesCorta, N'') NOT IN (N'OBSOLETO', N'ROLLUP')
        )
        BEGIN
            RAISERROR(
                N'El medio debe ser postable (Efectivo, Yape Gerente/René, Interbank, BCP, Nación, Plin). No use Yape rollup ni tipos de sede (6-8).',
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

/* ── 5) Resumen / cierres: incluyen hijos Yape (10/11) + legado 2 si queda ─ */
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
        IF NOT EXISTS (SELECT 1 FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaId)
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
        WHERE MC.BovedaId = @BovedaId AND MC.Estado = 1
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

/* ── 6) Resumen caja diario: mismas marcas (sin 6–8 / OBSOLETO) ──────────── */
CREATE OR ALTER FUNCTION [dbo].[ufnResumenCuentaCajaDiario] (@CajaDiarioId INT)
RETURNS VARCHAR(500)
AS
BEGIN
    DECLARE @SaldoInicial DECIMAL(15, 2) = (
        SELECT SaldoInicial FROM CREDITO.CajaDiario WHERE CajaDiarioId = @CajaDiarioId
    );
    DECLARE @Retorno VARCHAR(500);

    ;WITH SALDOINICIAL AS (
        SELECT
            ItemId AS TipoPagoId,
            Denominacion,
            IIF(ItemId = 1, @SaldoInicial, 0) AS Importe
        FROM MAESTRO.ValorTabla
        WHERE TablaId = 13
          AND ItemId > 0
          AND ItemId NOT BETWEEN 6 AND 8
          AND ISNULL(DesCorta, N'') <> N'OBSOLETO'
    ),
    CUENTAS AS (
        SELECT
            TipoPagoId,
            SUM(IIF(MC.IndEntrada = 1, MC.ImportePago, -MC.ImportePago)) AS Importe
        FROM CREDITO.MovimientoCaja AS MC
        WHERE MC.CajaDiarioId = @CajaDiarioId AND MC.Estado = 1
        GROUP BY MC.TipoPagoId
    ),
    SALDOS AS (
        SELECT
            S.TipoPagoId,
            S.Denominacion,
            S.Importe + ISNULL(c.Importe, 0) AS Saldo
        FROM SALDOINICIAL AS S
        LEFT JOIN CUENTAS AS c ON c.TipoPagoId = S.TipoPagoId
    )
    SELECT @Retorno = STRING_AGG(Denominacion + N' = ' + CAST(Saldo AS VARCHAR(15)), N'  ')
    FROM SALDOS
    WHERE Saldo <> 0;

    RETURN @Retorno;
END
GO

/* ── 7) Verificación ─────────────────────────────────────────────────────── */
SELECT ItemId, Denominacion, DesCorta, Valor
FROM MAESTRO.ValorTabla
WHERE TablaId = 13 AND ItemId IN (1, 2, 3, 4, 5, 9, 10, 11)
ORDER BY ItemId;

SELECT TipoPagoId, COUNT(1) AS Cuentas
FROM CREDITO.BovedaCuenta
WHERE TipoPagoId IN (2, 10, 11)
GROUP BY TipoPagoId
ORDER BY TipoPagoId;
GO
