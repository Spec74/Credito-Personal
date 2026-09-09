-- CREDITO.usp_ObtenerMontoPendientePlanPago
CREATE PROC CREDITO.usp_ObtenerMontoPendientePlanPago
@OficinaId INT =1
AS

DECLARE @fecha DATE = dbo.ufnFecha()

SELECT ISNULL(SUM(pp.Cuota-pp.PagoLibre),0) 'MontoPendiente'
FROM CREDITO.PlanPago PP
INNER JOIN CREDITO.Credito C ON C.CreditoId = PP.CreditoId
WHERE PP.Estado='PEN' AND C.Estado='DES' AND C.FechaVencimiento>=@fecha  AND C.OficinaId=@OficinaId
