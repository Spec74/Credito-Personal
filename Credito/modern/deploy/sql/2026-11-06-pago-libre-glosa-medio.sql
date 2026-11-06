-- Glosa de pago libre incluye medio (Yape/Plin/efectivo) para el reporte de movimientos.
SET NOCOUNT ON;
GO

CREATE OR ALTER PROC [CREDITO].[usp_PagarCuotaPagoLibre]
    @CajaDiarioId INT,
    @CreditoId INT,
    @ImporteRecibido DECIMAL(16, 2) = 0,
    @UsuarioId INT,
    @TipoPagoId INT = 1,
    @FechaPagoTransferencia VARCHAR(16) = ''
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalPago DECIMAL(16, 2) = 0, @IndImporteLibre BIT = 0;
    DECLARE @FechaActual DATETIME = dbo.ufnFecha();
    DECLARE @PersonaId INT, @MovimientoCajaId INT;
    DECLARE @MedioPago VARCHAR(80);

    SELECT @PersonaId = PersonaId FROM CREDITO.Credito WHERE CreditoId = @CreditoId;

    DECLARE @tCuotasPendientes TABLE (
        Id INT IDENTITY(1, 1),
        PlanPagoId INT,
        Glosa VARCHAR(MAX),
        FechaVencimiento DATE,
        Amortizacion DECIMAL(16, 2),
        Interes DECIMAL(16, 2),
        GastosAdm DECIMAL(16, 2),
        Cuota DECIMAL(16, 2),
        DiasAtrazo INT,
        ImporteMora DECIMAL(16, 2),
        Descuento DECIMAL(16, 2),
        Cargo DECIMAL(16, 2),
        PagoLibre DECIMAL(16, 2),
        PagoCuota DECIMAL(16, 2)
    );

    INSERT INTO @tCuotasPendientes
    EXEC CREDITO.usp_CuotasPendientes @CreditoId, @FechaActual;

    SELECT TOP (1) @MedioPago = vt.Denominacion
    FROM MAESTRO.ValorTabla AS vt
    WHERE vt.TablaId = 13 AND vt.ItemId = @TipoPagoId;

    INSERT INTO CREDITO.MovimientoCaja (
        CajaDiarioId, PersonaId, Operacion, ImportePago,
        Descripcion, IndEntrada, Estado, OrdenVentaId, CreditoId, UsuarioRegId, FechaReg, TipoPagoId
    )
    VALUES (
        @CajaDiarioId, @PersonaId, N'CUO', @ImporteRecibido,
        N'CREDITO ' + CAST(@CreditoId AS VARCHAR(20)) + N' PAGO LIBRE ' + CAST(@ImporteRecibido AS VARCHAR(20))
            + ISNULL(N' (' + @MedioPago + N')', N''),
        1, 1, NULL, @CreditoId, @UsuarioId, @FechaActual, @TipoPagoId
    );
    SET @MovimientoCajaId = @@IDENTITY;

    IF @TipoPagoId > 1
        INSERT INTO CREDITO.MovimientoCajaExtension (MovimientoCajaId, FechaTransferencia, IndTransferenciaVerificada)
        VALUES (@MovimientoCajaId, @FechaPagoTransferencia, 0);

    UPDATE PP
    SET PP.DiasAtrazo = CP.DiasAtrazo,
        PP.ImporteMora = CP.ImporteMora,
        PP.PagoCuota = CP.PagoCuota + CP.PagoLibre,
        PP.MovimientoCajaId = @MovimientoCajaId,
        PP.FechaPagoCuota = @FechaActual,
        PP.UsuarioModId = @UsuarioId,
        PP.FechaMod = @FechaActual
    FROM @tCuotasPendientes AS CP
    INNER JOIN CREDITO.PlanPago AS PP ON CP.PlanPagoId = PP.PlanPagoId
    WHERE PP.CreditoId = @CreditoId;

    EXEC CREDITO.usp_RegenerarPlanPago @CreditoId = @CreditoId;
    EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId = @CajaDiarioId;

    SELECT @MovimientoCajaId;
END
GO
