/*
  Azure CREDITO de prueba (no producción).

  Comparado con los ALTER de prod del 30/10/2026 18:44.

  usp_SimuladorCredito en prod solo salta domingo si FormaPago='D'.
  Emerson: D, S, Q y M — si la cuota cae domingo, pasa al lunes.
  Prendario no: 30 días corridos (EsPrendario=1 o ProductoId=2).

  Único cambio funcional:
    1) 7.º parámetro opcional @SaltarDomingo BIT = 1
       (MVC / EF siguen mandando 6 args → default 1, no se rompe).
    2) El IF de domingo deja de estar atado a diario.
    3) usp_Credito_Ins pasa 0 si es prendario.

  Interés, meses, amortización, tope, GA, central de riesgo: iguales a prod.
*/

USE [CREDITO];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROC [CREDITO].[usp_SimuladorCredito]
@Monto DECIMAL(16,2) = 0.0,
@FormaPago CHAR(1)='D',
@NroCuotas INT=26,
@InteresMensual DECIMAL(4,2)= 7,
@FechaPrimerPago DATE = '20140101',
@GastosAdm DECIMAL(16,2) = NULL,
@SaltarDomingo BIT = 1
AS


DECLARE @Sec INT=0,@PlazoMes INT ,@Capital DECIMAL(16,2)=@Monto,@FechaCuota DATE=@FechaPrimerPago, @InteresTotal DECIMAL(16,2)=0
DECLARE @Amortizacion DECIMAL(16,2)=@Monto/@NroCuotas,@Interes DECIMAL(16,2),@GastoAdmCuota DECIMAL(16,2)

-- 1. Se define la columna Saldo en la tabla temporal
DECLARE @tPlanPagos TABLE(Numero INT,Capital DECIMAL(16,2),FechaPago DATE,Amortizacion DECIMAL(16,2),Interes DECIMAL(16,2),GastosAdm DECIMAL(16,2),Cuota DECIMAL(16,2),Saldo DECIMAL(16,2))
DECLARE @Meses INT = 0
-- Variable para realizar la resta sucesiva
DECLARE @SaldoAcumulado DECIMAL(16,2) = @Monto

set datefirst 1

IF @SaltarDomingo IS NULL
    SET @SaltarDomingo = 1

IF @GastosAdm IS NULL
BEGIN
	SET @GastosAdm = 0
	SELECT @GastosAdm = @GastosAdm + CASE WHEN IndPorcentaje=1 THEN @monto*(Valor/100) ELSE valor END
	FROM CREDITO.GastosAdm
	WHERE Estado=1 AND @Monto BETWEEN MontoMinimo AND MontoMaximo
END
SET @GastoAdmCuota=@GastosAdm/@NroCuotas

IF @FormaPago='D' SET @Meses = CEILING(@NroCuotas/CAST(26 AS DECIMAL))
IF @FormaPago='S' SET @Meses = CEILING(@NroCuotas/CAST(4 AS DECIMAL))
IF @FormaPago='Q' SET @Meses = CEILING(@NroCuotas/CAST(2 AS DECIMAL))
IF @FormaPago='M' SET @Meses = @NroCuotas

SET @Interes = @Monto * ((@InteresMensual*@Meses/@NroCuotas)/100)
SET @InteresTotal = @Monto * ((@InteresMensual*@Meses)/100)

WHILE (@Sec<@NroCuotas)
BEGIN
	SET @Sec=@Sec+1

	-- Prod: solo IF @FormaPago='D'. Ahora D/S/Q/M si @SaltarDomingo=1.
	IF @SaltarDomingo = 1 AND datepart(dw, @FechaCuota) = 7
		SET @FechaCuota = DATEADD(DAY,1,@FechaCuota)

	-- 2. Restamos la amortización en cada cuota
	SET @SaldoAcumulado = @SaldoAcumulado - @Amortizacion

	INSERT INTO	@tPlanPagos(Numero,Capital,FechaPago,Amortizacion,Interes,GastosAdm,Saldo)
	VALUES		(@Sec,@Capital,@FechaCuota,@Amortizacion,@Interes,@GastoAdmCuota,@SaldoAcumulado)

	SET @Capital=@Capital-@Amortizacion

	IF @FormaPago='D' SET @FechaCuota = DATEADD(DAY,1,@FechaCuota)
	IF @FormaPago='S' SET @FechaCuota = DATEADD(DAY,7,@FechaCuota)
	IF @FormaPago='Q' SET @FechaCuota = DATEADD(DAY,15,@FechaCuota)
	IF @FormaPago='M' SET @FechaCuota = DATEADD(MONTH,1,@FechaCuota)
END

UPDATE	@tPlanPagos
SET		Amortizacion = Capital,
		GastosAdm = @GastosAdm - (@GastoAdmCuota * (@NroCuotas-1)),
		Interes = @InteresTotal - (@Interes * (@NroCuotas-1)),
		Saldo = 0.00 -- 3. Forzamos que la última cuota cierre con saldo cero
WHERE	Numero=@NroCuotas

UPDATE @tPlanPagos SET Cuota=Amortizacion+Interes+GastosAdm

SELECT * FROM @tPlanPagos
ORDER BY Numero
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

DECLARE @Mensaje VARCHAR(200)='', @MontoGA DECIMAL(16,2)=0, @Desembolso DECIMAL(16,2) = @MontoCredito,@CentralRiesgo DECIMAL(16,2) = 0

IF EXISTS(SELECT 1 FROM CREDITO.Credito WHERE CreditoId=@SolicitudCreditoId AND Estado<>'CRE')
BEGIN
 SET @Mensaje='ERROR: La Solicitud debe estar en estado CREADA'
 SELECT @Mensaje 'Mensaje'
 RETURN
END

DECLARE @TopeCredito Decimal(15,2) = (SELECT CL.TopeCredito FROM CREDITO.Credito C
										INNER JOIN MAESTRO.Cliente CL ON CL.PersonaId = C.PersonaId
										WHERE C.CreditoId= @SolicitudCreditoId)
IF @TopeCredito IS NOT NULL
BEGIN
	IF @MontoCredito>@TopeCredito
	BEGIN
	 SET @Mensaje='El tope maximo de credito para este cliente es: S/. ' + CAST(@TopeCredito AS VARCHAR(20))  + ' Si requiere mayor credito comuniquese con el administrador.'
	 SELECT @Mensaje 'Mensaje'
	 RETURN
	END
END

IF @IndGastoAdm='CUO'
 SET @MontoGA=@MontoGastosAdm

IF @IndGastoAdm='CAP'
 SET @Desembolso = @MontoCredito - @MontoGastosAdm

DECLARE @SaltarDomingo BIT = 1
IF EXISTS (
    SELECT 1
    FROM CREDITO.Credito
    WHERE CreditoId = @SolicitudCreditoId
      AND (ISNULL(EsPrendario, 0) = 1 OR ProductoId = 2)
)
    SET @SaltarDomingo = 0

/*CREACION PLAN PAGOS*/
DECLARE @tPlanPagos TABLE(Numero INT,Capital DECIMAL(16,2),FechaPago DATE,Amortizacion DECIMAL(16,2),Interes DECIMAL(16,2),GastosAdm DECIMAL(16,2),Cuota DECIMAL(16,2),Saldo DECIMAL(16,2))
INSERT INTO @tPlanPagos
EXEC CREDITO.usp_SimuladorCredito @MontoCredito, @FormaPago, @NroCuotas ,@Interes, @FechaPrimerPago,@MontoGA, @SaltarDomingo

INSERT INTO CREDITO.PlanPago ( CreditoId ,Numero ,Capital ,FechaVencimiento ,Amortizacion ,Interes,GastosAdm ,Cuota,Estado)
SELECT @SolicitudCreditoId 'CreditoId', Numero ,Capital ,FechaPago ,Amortizacion ,Interes,GastosAdm ,Cuota, 'CRE' 'Estado'
FROM @tPlanPagos ORDER BY Numero


IF @IndCentralRiesgo=1
BEGIN
 select @CentralRiesgo = CASE WHEN IndPorcentaje=1 THEN ((Valor * @MontoCredito)/100) ELSE Valor END
 from CREDITO.GastosAdm
 where @MontoCredito BETWEEN MontoMinimo and MontoMaximo
 and Ind=1
END

/*ACTUALIZAR CREDITO*/
UPDATE CREDITO.Credito
SET Estado='PEN' ,
FechaPrimerPago=@FechaPrimerPago,Interes=@Interes,
FormaPago=@FormaPago,NumeroCuotas=@NroCuotas,
MontoInicial=@MontoInicial,
MontoGastosAdm=@MontoGastosAdm - @CentralRiesgo,
CentralRiesgo=@CentralRiesgo,
MontoCredito=@MontoCredito,
MontoDesembolso = @Desembolso,
TipoGastoAdm=@IndGastoAdm,
ProductoId=@ProductoId,
Observacion=ISNULL(@Observacion,''),
TipoCuota=@TipoCuota,
FechaMod=dbo.ufnFecha(),UsuarioModId=@UsuarioId,
FechaVencimiento=(SELECT MAX(FechaPago) FROM @tPlanPagos)
WHERE CreditoId=@SolicitudCreditoId

SELECT @Mensaje 'Mensaje'
GO

/*
  Humo opcional (no inserta crédito). Domingo 1/11/2026:
  D 1 cuota → 2026-11-02
  S/Q/M 1 cuota → 2026-11-02
  M prendario (@SaltarDomingo=0) → 2026-11-01

EXEC CREDITO.usp_SimuladorCredito 1000,'D',1,5,'20261101',0,1;
EXEC CREDITO.usp_SimuladorCredito 1000,'S',1,5,'20261101',0,1;
EXEC CREDITO.usp_SimuladorCredito 1000,'Q',1,5,'20261101',0,1;
EXEC CREDITO.usp_SimuladorCredito 1000,'M',1,5,'20261101',0,1;
EXEC CREDITO.usp_SimuladorCredito 1000,'M',1,5,'20261101',0,0;
*/
