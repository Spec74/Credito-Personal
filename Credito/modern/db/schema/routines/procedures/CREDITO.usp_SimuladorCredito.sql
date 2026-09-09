
CREATE PROC [CREDITO].[usp_SimuladorCredito]
@Monto DECIMAL(16,2) = 0.0,
@FormaPago CHAR(1)='D', 
@NroCuotas INT=26, 
@InteresMensual DECIMAL(4,2)= 7,
@FechaPrimerPago DATE = '20140101',
@GastosAdm DECIMAL(16,2) = NULL
AS


DECLARE @Sec INT=0,@PlazoMes INT ,@Capital DECIMAL(16,2)=@Monto,@FechaCuota DATE=@FechaPrimerPago, @InteresTotal DECIMAL(16,2)=0
DECLARE @Amortizacion DECIMAL(16,2)=@Monto/@NroCuotas,@Interes DECIMAL(16,2),@GastoAdmCuota DECIMAL(16,2)

-- 1. Se define la columna Saldo en la tabla temporal
DECLARE @tPlanPagos TABLE(Numero INT,Capital DECIMAL(16,2),FechaPago DATE,Amortizacion DECIMAL(16,2),Interes DECIMAL(16,2),GastosAdm DECIMAL(16,2),Cuota DECIMAL(16,2),Saldo DECIMAL(16,2))
DECLARE @Meses INT = 0
-- Variable para realizar la resta sucesiva
DECLARE @SaldoAcumulado DECIMAL(16,2) = @Monto

set datefirst 1

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
	
	IF @FormaPago='D' 
		IF datepart(dw, @FechaCuota) = 7 
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
