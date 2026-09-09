-- Mora postergada (adicion del sistema moderno).
--
-- El flujo moderno permite registrar la mora de una cuota vencida antes de que el cliente
-- la pague. Esa mora queda "pendiente" y se representa con MovimientoCajaId NULL; al cobrarla,
-- usp_CreditoMora_Liquidar crea el movimiento de caja y vincula las filas.
--
-- DIVERGENCIA REGISTRADA: la entrega del cliente de 2026-09-01 define
-- CREDITO.CreditoMora.MovimientoCajaId como NOT NULL, lo que impide representar la mora
-- pendiente. Esta migracion vuelve la columna NULLABLE. Ver docs/migration/BITACORA-DESVIACIONES.md.
--
-- No existe en las entregas de base del cliente: reaplicar tras cada restauracion.

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'CREDITO.CreditoMora')
      AND name = N'MovimientoCajaId'
      AND is_nullable = 0)
BEGIN
    ALTER TABLE CREDITO.CreditoMora ALTER COLUMN MovimientoCajaId int NULL;
END
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_CreditoMora_Registrar]
    @CreditoId INT,
    @FechaVencimiento DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FechaActual DATE = dbo.ufnFecha();

    -- Estructura identica a la que devuelve usp_CuotasPendientes.
    DECLARE @tCuotasPendientes TABLE(
        Id INT IDENTITY(1,1), PlanPagoId INT, Glosa VARCHAR(MAX), FechaVencimiento DATE,
        Amortizacion DECIMAL(16,2), Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2),
        Cuota DECIMAL(16,2), DiasAtrazo INT, ImporteMora DECIMAL(16,2),
        Descuento DECIMAL(16,2), Cargo DECIMAL(16,2), PagoLibre DECIMAL(16,2), PagoCuota DECIMAL(16,2)
    );

    -- El calculo de atraso y mora lo hace el core, no se replica aqui.
    INSERT INTO @tCuotasPendientes
    EXEC CREDITO.usp_CuotasPendientes @CreditoId, @FechaActual;

    IF EXISTS (SELECT 1 FROM @tCuotasPendientes WHERE FechaVencimiento = @FechaVencimiento AND DiasAtrazo > 0)
    BEGIN
        DECLARE @DiasAtrazo REAL, @MontoMora DECIMAL(16,2);

        SELECT
            @DiasAtrazo = DiasAtrazo,
            @MontoMora = ImporteMora
        FROM @tCuotasPendientes
        WHERE FechaVencimiento = @FechaVencimiento;

        -- Evita duplicar la mora pendiente del mismo credito.
        IF @MontoMora > 0 AND NOT EXISTS (SELECT 1 FROM [CREDITO].[CreditoMora] WHERE CreditoId = @CreditoId AND MovimientoCajaId IS NULL)
        BEGIN
            INSERT INTO [CREDITO].[CreditoMora] (
                [CreditoId], [MovimientoCajaId], [Fecha], [Mora], [DiasAtrazo], [SaldoMora], [InteresMora]
            )
            VALUES (
                @CreditoId, NULL, GETDATE(), @MontoMora, @DiasAtrazo, @MontoMora, 0.00
            );
        END
    END
END
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_CreditoMora_Liquidar]
    @CreditoId INT,
    @CajaDiarioId INT,
    @UsuarioId INT,
    @TipoPagoId INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalMoraAPagar DECIMAL(16,2) =
        (SELECT ISNULL(SUM(SaldoMora), 0) FROM [CREDITO].[CreditoMora] WHERE [CreditoId] = @CreditoId AND [MovimientoCajaId] IS NULL);

    IF @TotalMoraAPagar > 0
    BEGIN
        DECLARE @PersonaId INT, @MovimientoCajaId INT;
        SELECT @PersonaId = PersonaId FROM CREDITO.Credito WHERE CreditoId = @CreditoId;

        INSERT INTO CREDITO.MovimientoCaja (
            CajaDiarioId, PersonaId, Operacion, ImportePago, Descripcion,
            IndEntrada, Estado, OrdenVentaId, CreditoId, UsuarioRegId, FechaReg, TipoPagoId
        )
        VALUES (
            @CajaDiarioId, @PersonaId, 'MOR', @TotalMoraAPagar,
            'CREDITO ' + CAST(@CreditoId AS VARCHAR(20)) + ' LIQUIDACION DE MORAS',
            1, 1, NULL, @CreditoId, @UsuarioId, GETDATE(), @TipoPagoId
        );

        -- SCOPE_IDENTITY evita capturar el identity de un trigger, a diferencia de @@IDENTITY.
        SET @MovimientoCajaId = CAST(SCOPE_IDENTITY() AS INT);

        UPDATE [CREDITO].[CreditoMora]
        SET
            [MovimientoCajaId] = @MovimientoCajaId,
            [SaldoMora] = 0.00
        WHERE
            [CreditoId] = @CreditoId
            AND [MovimientoCajaId] IS NULL;

        EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId;

        SELECT @MovimientoCajaId AS MovimientoCajaId;
    END
    ELSE
    BEGIN
        SELECT 0 AS MovimientoCajaId;
    END
END
GO
