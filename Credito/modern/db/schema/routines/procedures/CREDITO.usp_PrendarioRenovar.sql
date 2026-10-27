CREATE OR ALTER PROC [CREDITO].[usp_PrendarioRenovar]
    @CreditoId INT,
    @OficinaId INT,
    @CajaDiarioId INT,
    @UsuarioId INT,
    @Dias INT = 30,
    @TipoPagoId INT = 1,
    @FechaPagoTransferencia VARCHAR(16) = ''
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreditoId IS NULL OR @CreditoId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @CajaDiarioId IS NULL OR @CajaDiarioId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de renovación incompletos.', 16, 1);
        RETURN;
    END;

    IF @Dias IS NULL OR @Dias < 1
        SET @Dias = 30;
    IF @TipoPagoId IS NULL OR @TipoPagoId < 1
        SET @TipoPagoId = 1;
    IF @FechaPagoTransferencia IS NULL
        SET @FechaPagoTransferencia = '';

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.Credito AS c
        WHERE c.CreditoId = @CreditoId
          AND c.OficinaId = @OficinaId
          AND c.Estado = N'DES'
          AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
    )
    BEGIN
        RAISERROR(N'Crédito prendario desembolsado no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.CajaDiario AS cd
        INNER JOIN CREDITO.Caja AS cj ON cj.CajaId = cd.CajaId
        WHERE cd.CajaDiarioId = @CajaDiarioId
          AND cj.OficinaId = @OficinaId
          AND cd.IndCierre = CAST(0 AS bit)
          AND cd.FechaFinOperacion IS NULL
    )
    BEGIN
        RAISERROR(N'La caja no está abierta en esta sede.', 16, 1);
        RETURN;
    END;

    DECLARE @Interes DECIMAL(16, 2) = 0;
    DECLARE @PersonaId INT;
    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @MovimientoCajaId INT;

    SELECT @Interes = ISNULL(SUM(pp.Interes), 0)
    FROM CREDITO.PlanPago AS pp
    WHERE pp.CreditoId = @CreditoId
      AND pp.Estado = N'PEN';

    SELECT @PersonaId = c.PersonaId
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;

    IF @Interes > 0
    BEGIN
        INSERT INTO CREDITO.MovimientoCaja (
            CajaDiarioId, PersonaId, Operacion, ImportePago,
            Descripcion, IndEntrada, Estado, OrdenVentaId, CreditoId,
            UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (
            @CajaDiarioId, @PersonaId, N'CUO', @Interes,
            N'RENOVACION PRENDARIA INTERES CREDITO ' + CAST(@CreditoId AS varchar(20)),
            1, 1, NULL, @CreditoId, @UsuarioId, @Fecha, @TipoPagoId);

        SET @MovimientoCajaId = CONVERT(INT, SCOPE_IDENTITY());

        IF @TipoPagoId > 1
            INSERT INTO CREDITO.MovimientoCajaExtension (
                MovimientoCajaId, FechaTransferencia, IndTransferenciaVerificada)
            VALUES (@MovimientoCajaId, @FechaPagoTransferencia, 0);

        EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId = @CajaDiarioId;
    END;

    -- Calcular remate con el vencimiento actual. Si se prorroga antes, un FechaRemate
    -- nulo usa el vencimiento nuevo y suma @Dias otra vez (vence+30+@Dias).
    DECLARE @NuevaRemate DATE;
    SELECT @NuevaRemate = DATEADD(
        DAY,
        @Dias,
        COALESCE(c.FechaRemate, DATEADD(DAY, 30, c.FechaVencimiento)))
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;

    EXEC CREDITO.usp_ProrrogarCredito @CreditoId = @CreditoId, @Dias = @Dias;

    UPDATE CREDITO.Credito
    SET FechaRemate = @NuevaRemate,
        UsuarioModId = @UsuarioId,
        FechaMod = @Fecha
    WHERE CreditoId = @CreditoId
      AND OficinaId = @OficinaId;

    SELECT
        @CreditoId AS CreditoId,
        @Interes AS InteresCobrado,
        @Dias AS Dias,
        @MovimientoCajaId AS MovimientoCajaId,
        c.FechaVencimiento,
        c.FechaRemate
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;
END
