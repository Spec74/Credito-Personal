
-- CREDITO.usp_CalcularMoraPendiente 45996
CREATE PROC	[CREDITO].[usp_CalcularMoraPendiente]
@CreditoId INT

AS
DECLARE @Estado CHAR(3) = (SELECT Estado FROM CREDITO.Credito WHERE CreditoId=@CreditoId)

IF (@Estado = 'DES')
BEGIN
    DECLARE @FechaActual DATE = dbo.ufnFecha()
	DECLARE @FechaVencimiento DATE = (SELECT FechaVencimiento FROM CREDITO.Credito WHERE CreditoId=@CreditoId)
	DECLARE @DiasAtrazo INT = ( SELECT dbo.ufnCalcularDiasAtrazo(@FechaVencimiento,@FechaActual)) 							  

	DECLARE @MovCaja DECIMAL(15,2) = (SELECT SUM(ImportePago) FROM CREDITO.MovimientoCaja WHERE CreditoId=@CreditoId and ImportePago>0 and Operacion='CUO' and Estado=1)
	DECLARE @MontoTotalCredito DECIMAL(15,2) = (SELECT SUM(Cuota + Cargo) FROM CREDITO.PlanPago WHERE CreditoId=@CreditoId ) 
			  
	SELECT dbo.ufnCalcularMora((@MontoTotalCredito - ISNULL(@MovCaja,0)), @DiasAtrazo, 0) 'Mora'
END
ELSE
BEGIN
    SELECT 0.0 'Mora'	
END
