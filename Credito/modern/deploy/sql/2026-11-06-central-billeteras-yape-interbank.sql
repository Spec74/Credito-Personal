-- Multisucursal: billeteras Gerente/René SOLO en Central (IndPrincipal).
--   Yape:      10 GERENTE / 11 RENE  (Valor→2)   · genérico 2 en sucursales
--   Interbank: 12 GERENTE / 13 RENE  (Valor→3)   · genérico 3 en sucursales
--   Plin=9 para todas las sedes (sin cambio).
-- Corrige el ROLLUP global de Yape(2): vuelve postable en sucursal.
-- Migra saldos solo en bóvedas de la oficina Central.
-- Idempotente.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @OficinaCentralId INT = (
    SELECT TOP (1) OficinaId
    FROM MAESTRO.Oficina
    WHERE IndPrincipal = CAST(1 AS bit) AND Estado = CAST(1 AS bit)
    ORDER BY OficinaId
);

/* ── 1) Catálogo ─────────────────────────────────────────────────────────── */
-- Yape genérico: postable en sucursal (quitar ROLLUP si quedó del script anterior).
UPDATE MAESTRO.ValorTabla
SET Denominacion = N'YAPE',
    DesCorta = NULL,
    Valor = NULL
WHERE TablaId = 13 AND ItemId = 2;

UPDATE MAESTRO.ValorTabla
SET Denominacion = N'INTERBANK',
    DesCorta = NULL,
    Valor = NULL
WHERE TablaId = 13 AND ItemId = 3;

-- Yape billeteras Central
IF NOT EXISTS (SELECT 1 FROM MAESTRO.ValorTabla WHERE TablaId = 13 AND ItemId = 10)
    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
    VALUES (13, 10, N'YAPE GERENTE', N'CENTRAL', N'2');
ELSE
    UPDATE MAESTRO.ValorTabla
    SET Denominacion = N'YAPE GERENTE', DesCorta = N'CENTRAL', Valor = N'2'
    WHERE TablaId = 13 AND ItemId = 10;

IF NOT EXISTS (SELECT 1 FROM MAESTRO.ValorTabla WHERE TablaId = 13 AND ItemId = 11)
    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
    VALUES (13, 11, N'YAPE RENE', N'CENTRAL', N'2');
ELSE
    UPDATE MAESTRO.ValorTabla
    SET Denominacion = N'YAPE RENE', DesCorta = N'CENTRAL', Valor = N'2'
    WHERE TablaId = 13 AND ItemId = 11;

-- Interbank billeteras Central
IF NOT EXISTS (SELECT 1 FROM MAESTRO.ValorTabla WHERE TablaId = 13 AND ItemId = 12)
    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
    VALUES (13, 12, N'INTERBANK GERENTE', N'CENTRAL', N'3');
ELSE
    UPDATE MAESTRO.ValorTabla
    SET Denominacion = N'INTERBANK GERENTE', DesCorta = N'CENTRAL', Valor = N'3'
    WHERE TablaId = 13 AND ItemId = 12;

IF NOT EXISTS (SELECT 1 FROM MAESTRO.ValorTabla WHERE TablaId = 13 AND ItemId = 13)
    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
    VALUES (13, 13, N'INTERBANK RENE', N'CENTRAL', N'3');
ELSE
    UPDATE MAESTRO.ValorTabla
    SET Denominacion = N'INTERBANK RENE', DesCorta = N'CENTRAL', Valor = N'3'
    WHERE TablaId = 13 AND ItemId = 13;
GO

DECLARE @OficinaCentralId INT = (
    SELECT TOP (1) OficinaId
    FROM MAESTRO.Oficina
    WHERE IndPrincipal = CAST(1 AS bit) AND Estado = CAST(1 AS bit)
    ORDER BY OficinaId
);

/* ── 2) Cuentas billetera solo en bóvedas abiertas de Central ─────────────── */
IF @OficinaCentralId IS NOT NULL
BEGIN
    INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
    SELECT b.BovedaId, v.ItemId, CAST(0 AS decimal(16, 2))
    FROM CREDITO.Boveda AS b
    CROSS JOIN (VALUES (10), (11), (12), (13)) AS v(ItemId)
    WHERE b.IndCierre = CAST(0 AS bit)
      AND b.OficinaId = @OficinaCentralId
      AND NOT EXISTS (
          SELECT 1 FROM CREDITO.BovedaCuenta AS bc
          WHERE bc.BovedaId = b.BovedaId AND bc.TipoPagoId = v.ItemId
      );
END
GO

/* ── 3) Revertir migración errónea en SUCURSALES (10/11 → 2) si aplica ───── */
BEGIN TRAN;

DECLARE @UsuarioRegId INT = (
    SELECT TOP (1) UsuarioId FROM MAESTRO.Usuario WHERE Estado = 1 ORDER BY UsuarioId
);
DECLARE @OficinaCentralId INT = (
    SELECT TOP (1) OficinaId
    FROM MAESTRO.Oficina
    WHERE IndPrincipal = CAST(1 AS bit) AND Estado = CAST(1 AS bit)
    ORDER BY OficinaId
);

DECLARE @BovedaId INT;
DECLARE @Neto10 DECIMAL(16, 2);
DECLARE @Neto11 DECIMAL(16, 2);
DECLARE @Suma DECIMAL(16, 2);

DECLARE cSuc CURSOR LOCAL FAST_FORWARD FOR
SELECT b.BovedaId
FROM CREDITO.Boveda AS b
WHERE b.IndCierre = CAST(0 AS bit)
  AND (@OficinaCentralId IS NULL OR b.OficinaId <> @OficinaCentralId);

OPEN cSuc;
FETCH NEXT FROM cSuc INTO @BovedaId;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @Neto10 = CAST(
        ISNULL((SELECT SaldoInicial FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaId AND TipoPagoId = 10), 0)
        + ISNULL((SELECT SUM(IIF(IndEntrada = 1, Importe, -Importe)) FROM CREDITO.BovedaMov WHERE BovedaId = @BovedaId AND Estado = 1 AND TipoPagoId = 10), 0)
        AS decimal(16, 2));
    SELECT @Neto11 = CAST(
        ISNULL((SELECT SaldoInicial FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaId AND TipoPagoId = 11), 0)
        + ISNULL((SELECT SUM(IIF(IndEntrada = 1, Importe, -Importe)) FROM CREDITO.BovedaMov WHERE BovedaId = @BovedaId AND Estado = 1 AND TipoPagoId = 11), 0)
        AS decimal(16, 2));
    SET @Suma = CAST(ISNULL(@Neto10, 0) + ISNULL(@Neto11, 0) AS decimal(16, 2));

    IF @Suma <> 0
       AND NOT EXISTS (
           SELECT 1 FROM CREDITO.BovedaMov
           WHERE BovedaId = @BovedaId AND Estado = 1
             AND Glosa LIKE N'REVERT BILLETERA CENTRAL→YAPE%'
             AND TipoPagoId IN (10, 11)
       )
    BEGIN
        IF @Neto10 <> 0
        BEGIN
            INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
            VALUES (@BovedaId, N'TRS', N'REVERT BILLETERA CENTRAL→YAPE', ABS(@Neto10), IIF(@Neto10 > 0, 0, 1), 1, @UsuarioRegId, dbo.ufnFecha(), 10);
            INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
            VALUES (@BovedaId, N'TRE', N'REVERT BILLETERA CENTRAL→YAPE', ABS(@Neto10), IIF(@Neto10 > 0, 1, 0), 1, @UsuarioRegId, dbo.ufnFecha(), 2);
        END
        IF @Neto11 <> 0
        BEGIN
            INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
            VALUES (@BovedaId, N'TRS', N'REVERT BILLETERA CENTRAL→YAPE', ABS(@Neto11), IIF(@Neto11 > 0, 0, 1), 1, @UsuarioRegId, dbo.ufnFecha(), 11);
            INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
            VALUES (@BovedaId, N'TRE', N'REVERT BILLETERA CENTRAL→YAPE', ABS(@Neto11), IIF(@Neto11 > 0, 1, 0), 1, @UsuarioRegId, dbo.ufnFecha(), 2);
        END
        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId;
    END

    FETCH NEXT FROM cSuc INTO @BovedaId;
END
CLOSE cSuc;
DEALLOCATE cSuc;

/* ── 4) Central: migrar Yape(2)→Gerente(10) e Interbank(3)→Gerente(12) ──── */
DECLARE @Neto DECIMAL(16, 2);

DECLARE cCen CURSOR LOCAL FAST_FORWARD FOR
SELECT b.BovedaId
FROM CREDITO.Boveda AS b
WHERE b.IndCierre = CAST(0 AS bit)
  AND @OficinaCentralId IS NOT NULL
  AND b.OficinaId = @OficinaCentralId;

OPEN cCen;
FETCH NEXT FROM cCen INTO @BovedaId;
WHILE @@FETCH_STATUS = 0
BEGIN
    -- Yape → 10
    SELECT @Neto = CAST(
        ISNULL((SELECT SaldoInicial FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaId AND TipoPagoId = 2), 0)
        + ISNULL((SELECT SUM(IIF(IndEntrada = 1, Importe, -Importe)) FROM CREDITO.BovedaMov WHERE BovedaId = @BovedaId AND Estado = 1 AND TipoPagoId = 2), 0)
        AS decimal(16, 2));
    IF @Neto <> 0
       AND NOT EXISTS (
           SELECT 1 FROM CREDITO.BovedaMov
           WHERE BovedaId = @BovedaId AND Estado = 1
             AND Glosa LIKE N'MIGRACION YAPE→GERENTE%' AND TipoPagoId = 2
             AND ABS(Importe - ABS(@Neto)) < 0.005
       )
    BEGIN
        INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (@BovedaId, N'TRS', N'MIGRACION YAPE→GERENTE (Central)', ABS(@Neto), IIF(@Neto > 0, 0, 1), 1, @UsuarioRegId, dbo.ufnFecha(), 2);
        INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (@BovedaId, N'TRE', N'MIGRACION YAPE→GERENTE (Central)', ABS(@Neto), IIF(@Neto > 0, 1, 0), 1, @UsuarioRegId, dbo.ufnFecha(), 10);
    END

    -- Interbank → 12
    SELECT @Neto = CAST(
        ISNULL((SELECT SaldoInicial FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaId AND TipoPagoId = 3), 0)
        + ISNULL((SELECT SUM(IIF(IndEntrada = 1, Importe, -Importe)) FROM CREDITO.BovedaMov WHERE BovedaId = @BovedaId AND Estado = 1 AND TipoPagoId = 3), 0)
        AS decimal(16, 2));
    IF @Neto <> 0
       AND NOT EXISTS (
           SELECT 1 FROM CREDITO.BovedaMov
           WHERE BovedaId = @BovedaId AND Estado = 1
             AND Glosa LIKE N'MIGRACION INTERBANK→GERENTE%' AND TipoPagoId = 3
             AND ABS(Importe - ABS(@Neto)) < 0.005
       )
    BEGIN
        INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (@BovedaId, N'TRS', N'MIGRACION INTERBANK→GERENTE (Central)', ABS(@Neto), IIF(@Neto > 0, 0, 1), 1, @UsuarioRegId, dbo.ufnFecha(), 3);
        INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (@BovedaId, N'TRE', N'MIGRACION INTERBANK→GERENTE (Central)', ABS(@Neto), IIF(@Neto > 0, 1, 0), 1, @UsuarioRegId, dbo.ufnFecha(), 12);
    END

    EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId;
    FETCH NEXT FROM cCen INTO @BovedaId;
END
CLOSE cCen;
DEALLOCATE cCen;

COMMIT TRAN;
GO

/* ── 5) Transferir: postable = no OBSOLETO / no 6-8 (billeteras CENTRAL OK) ─ */
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
    IF ISNULL(@TipoPagoId, 0) < 1 SET @TipoPagoId = 1;

    IF @flagAceptar = 0
    BEGIN
        IF @BovedaInicioId IS NULL OR @BovedaDestinoId IS NULL OR @BovedaInicioId < 1 OR @BovedaDestinoId < 1
        BEGIN RAISERROR(N'Origen y destino de bóveda son obligatorios.', 16, 1); RETURN; END
        IF @BovedaInicioId = @BovedaDestinoId
        BEGIN RAISERROR(N'La bóveda de origen y destino no pueden ser la misma.', 16, 1); RETURN; END
        IF @Monto IS NULL OR @Monto <= 0
        BEGIN RAISERROR(N'El monto debe ser mayor a cero.', 16, 1); RETURN; END

        IF NOT EXISTS (
            SELECT 1 FROM MAESTRO.ValorTabla AS vt
            WHERE vt.TablaId = 13 AND vt.ItemId = @TipoPagoId
              AND vt.ItemId > 0 AND vt.ItemId NOT BETWEEN 6 AND 8
              AND ISNULL(vt.DesCorta, N'') <> N'OBSOLETO'
        )
        BEGIN
            RAISERROR(N'El medio no es una marca activa.', 16, 1);
            RETURN;
        END

        -- Billetera CENTRAL solo en bóveda de oficina principal.
        IF EXISTS (
            SELECT 1 FROM MAESTRO.ValorTabla
            WHERE TablaId = 13 AND ItemId = @TipoPagoId AND DesCorta = N'CENTRAL'
        )
        AND NOT EXISTS (
            SELECT 1
            FROM CREDITO.Boveda AS b
            INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = b.OficinaId
            WHERE b.BovedaId = @BovedaInicioId AND o.IndPrincipal = CAST(1 AS bit)
        )
        BEGIN
            RAISERROR(N'Yape/Interbank Gerente y René solo aplican en la bóveda Central.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = @BovedaInicioId AND IndCierre = 0)
        BEGIN RAISERROR(N'La bóveda de origen no está abierta.', 16, 1); RETURN; END
        IF NOT EXISTS (SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = @BovedaDestinoId AND IndCierre = 0)
        BEGIN RAISERROR(N'La bóveda de destino no está abierta.', 16, 1); RETURN; END

        DECLARE @HayCuentas BIT = CASE WHEN EXISTS (SELECT 1 FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaInicioId) THEN 1 ELSE 0 END;
        DECLARE @SiBoveda DECIMAL(16, 2) = (SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId = @BovedaInicioId);
        DECLARE @SiMedio DECIMAL(16, 2) = ISNULL((SELECT SaldoInicial FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaInicioId AND TipoPagoId = @TipoPagoId), 0);
        IF @HayCuentas = 0 AND @TipoPagoId = 1 SET @SiMedio = ISNULL(@SiBoveda, 0);
        DECLARE @NetoMedio DECIMAL(16, 2) = ISNULL((
            SELECT SUM(IIF(IndEntrada = 1, Importe, -Importe))
            FROM CREDITO.BovedaMov
            WHERE BovedaId = @BovedaInicioId AND Estado = 1 AND TipoPagoId = @TipoPagoId
        ), 0);
        IF (@SiMedio + @NetoMedio) < @Monto
        BEGIN RAISERROR(N'Saldo insuficiente en el medio de origen.', 16, 1); RETURN; END

        INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (@BovedaInicioId, N'TRS', @Glosa, @Monto, 0, @Estado, @UsuarioRegId, @FechaReg, @TipoPagoId);
        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId;
        INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (@BovedaDestinoId, N'TRE', @Glosa, @Monto, 1, @Estado, @UsuarioRegId, @FechaReg, @TipoPagoId);
        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaDestinoId;
    END

    IF @flagAceptar = 2
    BEGIN
        DELETE FROM CREDITO.BovedaMov WHERE MovimientoBovedaId = (
            SELECT MovimientoBovedaIniId FROM CREDITO.BovedaMovTemp WHERE BovedaMovTempId = @BovedaMovTempId);
        SET @BovedaInicioId = (SELECT BovedaInicioId FROM CREDITO.BovedaMovTemp WHERE BovedaMovTempId = @BovedaMovTempId);
        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId;
        DELETE FROM CREDITO.BovedaMovTemp WHERE BovedaMovTempId = @BovedaMovTempId;
    END
END
GO

SELECT ItemId, Denominacion, DesCorta, Valor
FROM MAESTRO.ValorTabla
WHERE TablaId = 13 AND ItemId IN (1, 2, 3, 4, 5, 9, 10, 11, 12, 13)
ORDER BY ItemId;
GO
