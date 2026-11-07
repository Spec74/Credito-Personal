-- Transferencia interoficina con billeteras Central (10–13):
--   Fondeo Central→sucursal: sale 10–13, llega familia genérica (2/3).
--   Remesa sucursal→Central: elige billetera destino (10–13); sale familia (2/3).
-- Evita saldos huérfanos 10–13 fuera de IndPrincipal.
-- Idempotente (CREATE OR ALTER).

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_TransferirBoveda](
    @BovedaInicioId INT,
    @BovedaDestinoId INT,
    @Glosa VARCHAR(MAX),
    @Monto DECIMAL(16, 2),
    @UsuarioRegId INT,
    @flagAceptar INT,
    @BovedaMovTempId INT = 0,
    @TipoPagoId SMALLINT = 1)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Estado BIT = 1;
    DECLARE @FechaReg DATETIME = dbo.ufnFecha();
    DECLARE @TipoPagoOrigenId SMALLINT;
    DECLARE @TipoPagoDestinoId SMALLINT;
    DECLARE @OrigenEsCentral BIT;
    DECLARE @DestinoEsCentral BIT;
    DECLARE @EsBilleteraCentral BIT;

    -- Fail-fast: no embudo silencioso a efectivo (pérdida de medio en multisucursal).
    IF ISNULL(@TipoPagoId, 0) < 1
    BEGIN
        RAISERROR(N'TipoPagoId es obligatorio; no se asume efectivo.', 16, 1);
        RETURN;
    END

    SET @TipoPagoOrigenId = @TipoPagoId;
    SET @TipoPagoDestinoId = @TipoPagoId;

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
            RAISERROR(N'El medio no es una marca activa.', 16, 1);
            RETURN;
        END

        SELECT @OrigenEsCentral = CAST(ISNULL(o.IndPrincipal, 0) AS bit)
        FROM CREDITO.Boveda AS b
        INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = b.OficinaId
        WHERE b.BovedaId = @BovedaInicioId;

        SELECT @DestinoEsCentral = CAST(ISNULL(o.IndPrincipal, 0) AS bit)
        FROM CREDITO.Boveda AS b
        INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = b.OficinaId
        WHERE b.BovedaId = @BovedaDestinoId;

        IF @OrigenEsCentral IS NULL OR @DestinoEsCentral IS NULL
        BEGIN
            RAISERROR(N'No se pudo resolver la oficina de origen o destino.', 16, 1);
            RETURN;
        END

        SET @EsBilleteraCentral = CASE
            WHEN EXISTS (
                SELECT 1 FROM MAESTRO.ValorTabla
                WHERE TablaId = 13 AND ItemId = @TipoPagoId AND DesCorta = N'CENTRAL')
            THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;

        IF @EsBilleteraCentral = CAST(1 AS bit)
        BEGIN
            -- Fondeo: Central tipa billetera → sucursal recibe genérico.
            IF @OrigenEsCentral = CAST(1 AS bit) AND @DestinoEsCentral = CAST(0 AS bit)
            BEGIN
                SET @TipoPagoDestinoId = CASE
                    WHEN @TipoPagoId IN (10, 11) THEN CAST(2 AS smallint)
                    WHEN @TipoPagoId IN (12, 13) THEN CAST(3 AS smallint)
                    ELSE @TipoPagoId
                END;
            END
            -- Remesa: sucursal elige billetera destino → sale genérico de familia.
            ELSE IF @OrigenEsCentral = CAST(0 AS bit) AND @DestinoEsCentral = CAST(1 AS bit)
            BEGIN
                SET @TipoPagoOrigenId = CASE
                    WHEN @TipoPagoId IN (10, 11) THEN CAST(2 AS smallint)
                    WHEN @TipoPagoId IN (12, 13) THEN CAST(3 AS smallint)
                    ELSE @TipoPagoId
                END;
            END
            ELSE
            BEGIN
                RAISERROR(
                    N'Yape/Interbank Gerente y René solo aplican en fondeo Central→sucursal o remesa sucursal→Central.',
                    16, 1);
                RETURN;
            END
        END
        ELSE IF @TipoPagoId IN (2, 3)
             AND @OrigenEsCentral = CAST(1 AS bit)
        BEGIN
            RAISERROR(
                N'En Central use Yape/Interbank Gerente o René (el genérico es el total de red).',
                16, 1);
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = @BovedaInicioId AND IndCierre = 0)
        BEGIN
            RAISERROR(N'La bóveda de origen no está abierta.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = @BovedaDestinoId AND IndCierre = 0)
        BEGIN
            RAISERROR(N'La bóveda de destino no está abierta.', 16, 1);
            RETURN;
        END

        DECLARE @HayCuentas BIT = CASE
            WHEN EXISTS (SELECT 1 FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaInicioId)
            THEN 1 ELSE 0 END;
        DECLARE @SiBoveda DECIMAL(16, 2) = (
            SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId = @BovedaInicioId);
        DECLARE @SiMedio DECIMAL(16, 2) = ISNULL((
            SELECT SaldoInicial
            FROM CREDITO.BovedaCuenta
            WHERE BovedaId = @BovedaInicioId AND TipoPagoId = @TipoPagoOrigenId), 0);
        IF @HayCuentas = 0 AND @TipoPagoOrigenId = 1
            SET @SiMedio = ISNULL(@SiBoveda, 0);
        DECLARE @NetoMedio DECIMAL(16, 2) = ISNULL((
            SELECT SUM(IIF(IndEntrada = 1, Importe, -Importe))
            FROM CREDITO.BovedaMov
            WHERE BovedaId = @BovedaInicioId AND Estado = 1 AND TipoPagoId = @TipoPagoOrigenId), 0);

        IF (@SiMedio + @NetoMedio) < @Monto
        BEGIN
            RAISERROR(N'Saldo insuficiente en el medio de origen.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (
            SELECT 1 FROM CREDITO.BovedaCuenta
            WHERE BovedaId = @BovedaDestinoId AND TipoPagoId = @TipoPagoDestinoId
        )
        BEGIN
            INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
            VALUES (@BovedaDestinoId, @TipoPagoDestinoId, CAST(0 AS decimal(16, 2)));
        END

        INSERT INTO CREDITO.BovedaMov (
            BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (
            @BovedaInicioId, 'TRS', @Glosa, @Monto, 0, @Estado, @UsuarioRegId, @FechaReg, @TipoPagoOrigenId);
        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId;

        INSERT INTO CREDITO.BovedaMov (
            BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (
            @BovedaDestinoId, 'TRE', @Glosa, @Monto, 1, @Estado, @UsuarioRegId, @FechaReg, @TipoPagoDestinoId);
        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaDestinoId;
    END

    IF @flagAceptar = 2
    BEGIN
        DELETE CREDITO.BovedaMov
        WHERE MovimientoBovedaId = (
            SELECT MovimientoBovedaIniId FROM CREDITO.BovedaMovTemp WHERE BovedaMovTempId = @BovedaMovTempId);

        SET @BovedaInicioId = (
            SELECT BovedaInicioId FROM CREDITO.BovedaMovTemp WHERE BovedaMovTempId = @BovedaMovTempId);

        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId;

        DELETE CREDITO.BovedaMovTemp WHERE BovedaMovTempId = @BovedaMovTempId;
    END
END
GO
