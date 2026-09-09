
CREATE   PROCEDURE [CREDITO].[usp_CreditoMora_Registrar]
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
