/*
  Gerencia 30/10/2026 (Emerson): en diario, semanal, quincenal y mensual
  si la cuota cae domingo se pasa al lunes. Prendario sigue con domingo
  incluido (30 días corridos).

  Azure CREDITO de prueba primero. El 7.º parámetro es opcional (default 1)
  para no romper MVC / EF.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROC [CREDITO].[usp_SimuladorCredito]
    @Monto DECIMAL(16,2) = 0.0,
    @FormaPago CHAR(1) = 'D',
    @NroCuotas INT = 26,
    @InteresMensual DECIMAL(4,2) = 7,
    @FechaPrimerPago DATE = '20140101',
    @GastosAdm DECIMAL(16,2) = NULL,
    @SaltarDomingo BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Sec INT = 0,
            @Capital DECIMAL(16,2) = @Monto,
            @FechaCuota DATE = @FechaPrimerPago,
            @InteresTotal DECIMAL(16,2) = 0;
    DECLARE @Amortizacion DECIMAL(16,2) = @Monto / @NroCuotas,
            @Interes DECIMAL(16,2),
            @GastoAdmCuota DECIMAL(16,2);

    DECLARE @tPlanPagos TABLE (
        Numero INT,
        Capital DECIMAL(16,2),
        FechaPago DATE,
        Amortizacion DECIMAL(16,2),
        Interes DECIMAL(16,2),
        GastosAdm DECIMAL(16,2),
        Cuota DECIMAL(16,2),
        Saldo DECIMAL(16,2)
    );
    DECLARE @Meses INT = 0;
    DECLARE @SaldoAcumulado DECIMAL(16,2) = @Monto;

    SET DATEFIRST 1;

    IF @SaltarDomingo IS NULL
        SET @SaltarDomingo = 1;

    IF @GastosAdm IS NULL
    BEGIN
        SET @GastosAdm = 0;
        SELECT @GastosAdm = @GastosAdm + CASE WHEN IndPorcentaje = 1 THEN @Monto * (Valor / 100) ELSE Valor END
        FROM CREDITO.GastosAdm
        WHERE Estado = 1
          AND @Monto BETWEEN MontoMinimo AND MontoMaximo;
    END;
    SET @GastoAdmCuota = @GastosAdm / @NroCuotas;

    IF @FormaPago = 'D' SET @Meses = CEILING(@NroCuotas / CAST(26 AS DECIMAL));
    IF @FormaPago = 'S' SET @Meses = CEILING(@NroCuotas / CAST(4 AS DECIMAL));
    IF @FormaPago = 'Q' SET @Meses = CEILING(@NroCuotas / CAST(2 AS DECIMAL));
    IF @FormaPago = 'M' SET @Meses = @NroCuotas;

    SET @Interes = @Monto * ((@InteresMensual * @Meses / @NroCuotas) / 100);
    SET @InteresTotal = @Monto * ((@InteresMensual * @Meses) / 100);

    WHILE (@Sec < @NroCuotas)
    BEGIN
        SET @Sec = @Sec + 1;

        IF @SaltarDomingo = 1 AND DATEPART(dw, @FechaCuota) = 7
            SET @FechaCuota = DATEADD(DAY, 1, @FechaCuota);

        SET @SaldoAcumulado = @SaldoAcumulado - @Amortizacion;

        INSERT INTO @tPlanPagos (Numero, Capital, FechaPago, Amortizacion, Interes, GastosAdm, Saldo)
        VALUES (@Sec, @Capital, @FechaCuota, @Amortizacion, @Interes, @GastoAdmCuota, @SaldoAcumulado);

        SET @Capital = @Capital - @Amortizacion;

        IF @FormaPago = 'D' SET @FechaCuota = DATEADD(DAY, 1, @FechaCuota);
        IF @FormaPago = 'S' SET @FechaCuota = DATEADD(DAY, 7, @FechaCuota);
        IF @FormaPago = 'Q' SET @FechaCuota = DATEADD(DAY, 15, @FechaCuota);
        IF @FormaPago = 'M' SET @FechaCuota = DATEADD(MONTH, 1, @FechaCuota);
    END;

    UPDATE @tPlanPagos
    SET Amortizacion = Capital,
        GastosAdm = @GastosAdm - (@GastoAdmCuota * (@NroCuotas - 1)),
        Interes = @InteresTotal - (@Interes * (@NroCuotas - 1)),
        Saldo = 0.00
    WHERE Numero = @NroCuotas;

    UPDATE @tPlanPagos SET Cuota = Amortizacion + Interes + GastosAdm;

    SELECT * FROM @tPlanPagos ORDER BY Numero;
END;
GO

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

    DECLARE @SaltarDomingo BIT = 1;
    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito
        WHERE CreditoId = @SolicitudCreditoId
          AND ISNULL(EsPrendario, 0) = 1
    )
        SET @SaltarDomingo = 0;

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
        @MontoGA,
        @SaltarDomingo;

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
GO
