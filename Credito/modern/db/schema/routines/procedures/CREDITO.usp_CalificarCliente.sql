-- exec CREDITO.usp_CalificarCliente 

CREATE PROC [CREDITO].[usp_CalificarCliente]
@OficinaId INT = NULL 
AS

DECLARE @FechaAct DATE= dbo.ufnFecha()

;WITH DESEMBOLSOS AS(
	SELECT	C.CreditoId,
			dbo.ufnCalcularDiasAtrazo(
				(SELECT MIN(FechaVencimiento) FROM CREDITO.PlanPago WHERE CreditoId=C.CreditoId AND Estado='PEN')
				,@FechaAct) 'DiasAtrazo'
	FROM	CREDITO.Credito C
	WHERE	C.Estado='DES' AND C.OficinaId=ISNULL(@OficinaId,C.OficinaId) 
)
UPDATE C
SET C.Calificacion = dbo.ufnCalificar(CM.DiasAtrazo,C.Calificacion)
--SELECT	CM.*,dbo.ufnCalificar(CM.DiasAtrazo,C.Calificacion)
FROM CREDITO.Credito C
INNER JOIN DESEMBOLSOS CM ON C.CreditoId = CM.CreditoId

	UPDATE CL
	SET CL.Calificacion=C.Calificacion
	FROM	CREDITO.Credito C
	INNER JOIN MAESTRO.Cliente CL ON C.PersonaId=CL.PersonaId
	WHERE	C.Estado='DES' AND C.OficinaId=ISNULL(@OficinaId,C.OficinaId)
