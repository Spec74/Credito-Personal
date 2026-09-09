

/*
SELECT * FROM CREDITO.PlanPago WHERE CreditoId=16
EXEC [CREDITO].[usp_PagarCuotasCancelacion] 2,16,3,'20140311'
SELECT * FROM CREDITO.PlanPago WHERE CreditoId=13
SELECT * FROM CREDITO.CreditoPago

*/
CREATE PROC [CREDITO].[usp_PagarCuotasCancelacion]
@CajaDiarioId INT ,
@CreditoId INT,
@UsuarioId INT,
@FechaPago DATE=NULL
AS
DECLARE @FechaActual DATETIME=dbo.ufnFecha()
DECLARE @ListaPlanPagoId VARCHAR(MAX)='',@PagoCuota DECIMAL(16,2)=0,@PlanPagoId INT,@Index INT=1, @SumaPagoCuota DECIMAL(16,2)=0,
		@NroCuotas INT=0

IF EXISTS(SELECT 1 FROM CREDITO.Credito WITH(NOLOCK) WHERE Estado<>'DES' AND CreditoId=@CreditoId)
	RETURN
	
IF @FechaPago IS NULL
	SET @FechaPago=@FechaActual
	
DECLARE @tCuotasPendientes TABLE(Id INT IDENTITY(1,1),PlanPagoId INT,Glosa VARCHAR(MAX),FechaVencimiento DATE,Amortizacion DECIMAL(16,2),
								Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2), Cuota DECIMAL(16,2), DiasAtrazo INT,
								ImporteMora DECIMAL(16,2),Descuento DECIMAL(16,2), Cargo DECIMAL(16,2),
								PagoLibre DECIMAL(16,2),PagoCuota DECIMAL(16,2))
INSERT INTO @tCuotasPendientes
EXEC CREDITO.usp_CuotasPendientes @CreditoId,@FechaPago,1

UPDATE	PP
SET		Interes = P.Interes ,Cuota=P.Cuota ,PagoCuota=P.PagoCuota 
FROM	CREDITO.PlanPago PP 
INNER JOIN @tCuotasPendientes P ON PP.PlanPagoId=P.PlanPagoId

SET @NroCuotas = (SELECT COUNT(1) FROM @tCuotasPendientes)

WHILE @Index<=@NroCuotas
BEGIN
	SELECT @PagoCuota=PagoCuota, @PlanPagoId=PlanPagoId 
	FROM @tCuotasPendientes WHERE Id=@Index
	
	SET @SumaPagoCuota=@SumaPagoCuota + @PagoCuota
	SET @ListaPlanPagoId = @ListaPlanPagoId + CAST(@PlanPagoId AS VARCHAR(10)) + ','
	SET @Index = @Index + 1
END

IF LEN(@ListaPlanPagoId)>0
BEGIN
	SET @ListaPlanPagoId = SUBSTRING(@ListaPlanPagoId,1,LEN(@ListaPlanPagoId)-1)
	--SELECT @SumaPagoCuota, @ListaPlanPagoId 
	EXEC CREDITO.usp_PagarCuotas @CajaDiarioId,@CreditoId,@ListaPlanPagoId,@SumaPagoCuota,@UsuarioId,@FechaPago
END
