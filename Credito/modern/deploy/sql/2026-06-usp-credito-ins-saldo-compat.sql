CREATE OR ALTER PROC [CREDITO].[usp_Credito_Ins]
@SolicitudCreditoId INT,
@ProductoId INT,
@TipoCuota CHAR(1),
@MontoInicial DECIMAL(16,2),
@MontoCredito DECIMAL(16,2),
@MontoGastosAdm DECIMAL(16,2),
@IndGastoAdm CHAR(3),
@FormaPago CHAR(1),
@NroCuotas INT,
@Interes DECIMAL(4,2),
@FechaPrimerPago DATE,
@Observacion VARCHAR(MAX),
@UsuarioId INT,
@IndCentralRiesgo BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Mensaje VARCHAR(200) = '',
            @MontoGA DECIMAL(16,2) = 0,
            @Desembolso DECIMAL(16,2) = @MontoCredito,
            @CentralRiesgo DECIMAL(16,2) = 0;

    IF EXISTS(SELECT 1 FROM CREDITO.Credito WHERE CreditoId = @SolicitudCreditoId AND Estado <> 'CRE')
    BEGIN
        SET @Mensaje = 'ERROR: La Solicitud debe estar en estado CREADA';
        SELECT @Mensaje AS Mensaje;
        RETURN;
    END;

    DECLARE @TopeCredito DECIMAL(15,2) = (
        SELECT CL.TopeCredito
        FROM CREDITO.Credito C
        INNER JOIN MAESTRO.Cliente CL ON CL.PersonaId = C.PersonaId
        WHERE C.CreditoId = @SolicitudCreditoId
    );

    IF @TopeCredito IS NOT NULL AND @MontoCredito > @TopeCredito
    BEGIN
        SET @Mensaje = 'El tope maximo de credito para este cliente es: S/. '
            + CAST(@TopeCredito AS VARCHAR(20))
            + ' Si requiere mayor credito comuniquese con el administrador.';
        SELECT @Mensaje AS Mensaje;
        RETURN;
    END;

    IF @IndGastoAdm = 'CUO'
        SET @MontoGA = @MontoGastosAdm;

    IF @IndGastoAdm = 'CAP'
        SET @Desembolso = @MontoCredito - @MontoGastosAdm;

    DECLARE @tPlanPagos TABLE(
        Numero INT,
        Capital DECIMAL(16,2),
        FechaPago DATE,
        Amortizacion DECIMAL(16,2),
        Interes DECIMAL(16,2),
        GastosAdm DECIMAL(16,2),
        Cuota DECIMAL(16,2),
        Saldo DECIMAL(16,2) NULL
    );

    INSERT INTO @tPlanPagos
    EXEC CREDITO.usp_SimuladorCredito
        @MontoCredito,
        @FormaPago,
        @NroCuotas,
        @Interes,
        @FechaPrimerPago,
        @MontoGA;

    INSERT INTO CREDITO.PlanPago (
        CreditoId,
        Numero,
        Capital,
        FechaVencimiento,
        Amortizacion,
        Interes,
        GastosAdm,
        Cuota,
        Estado)
    SELECT
        @SolicitudCreditoId,
        Numero,
        Capital,
        FechaPago,
        Amortizacion,
        Interes,
        GastosAdm,
        Cuota,
        'CRE'
    FROM @tPlanPagos
    ORDER BY Numero;

    IF @IndCentralRiesgo = 1
    BEGIN
        SELECT @CentralRiesgo = CASE WHEN IndPorcentaje = 1 THEN ((Valor * @MontoCredito) / 100) ELSE Valor END
        FROM CREDITO.GastosAdm
        WHERE @MontoCredito BETWEEN MontoMinimo AND MontoMaximo
          AND Ind = 1;
    END;

    UPDATE CREDITO.Credito
    SET Estado = 'PEN',
        FechaPrimerPago = @FechaPrimerPago,
        Interes = @Interes,
        FormaPago = @FormaPago,
        NumeroCuotas = @NroCuotas,
        MontoInicial = @MontoInicial,
        MontoGastosAdm = @MontoGastosAdm - @CentralRiesgo,
        CentralRiesgo = @CentralRiesgo,
        MontoCredito = @MontoCredito,
        MontoDesembolso = @Desembolso,
        TipoGastoAdm = @IndGastoAdm,
        ProductoId = @ProductoId,
        Observacion = ISNULL(@Observacion, ''),
        TipoCuota = @TipoCuota,
        FechaMod = dbo.ufnFecha(),
        UsuarioModId = @UsuarioId,
        FechaVencimiento = (SELECT MAX(FechaPago) FROM @tPlanPagos)
    WHERE CreditoId = @SolicitudCreditoId;

    SELECT @Mensaje AS Mensaje;
END;
