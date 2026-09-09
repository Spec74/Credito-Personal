
CREATE PROC CREDITO.[usp_SolicitudCredito_Del]
@CreditoId INT=0
AS
BEGIN

	DELETE FROM CREDITO.PlanPago WHERE CreditoId=@CreditoId
	DELETE FROM CREDITO.Credito WHERE CreditoId=@CreditoId
	
END
