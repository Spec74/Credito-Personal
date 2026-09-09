
-- CREDITO.uspCreditoVencido 53400
CREATE PROC [CREDITO].[uspCreditoVencido]
@CreditoId INT =NULL
AS

DECLARE @Hoy DATE = dbo.ufnFecha()
DECLARE @AgenteIrrecuperable INT = (SELECT UsuarioId FROM MAESTRO.Usuario WHERE NombreUsuario='IRRECUPERABLE')


SELECT
    SUM(pp.Cuota - pp.PagoLibre) 'CreditoVencido',
    SUM(CASE WHEN c.FechaVencimiento BETWEEN DATEADD(DAY, -60, @Hoy) AND @Hoy THEN pp.Cuota - pp.PagoLibre ELSE 0 END) 'VencidoMenor60',
    SUM(CASE WHEN c.FechaVencimiento < DATEADD(DAY, -60, @Hoy) THEN pp.Cuota - pp.PagoLibre ELSE 0 END) 'VencidoMayor60',
	SUM(CASE WHEN c.UsuarioRegId = @AgenteIrrecuperable   THEN pp.Cuota - pp.PagoLibre ELSE 0 END) 'VencidoIrrecuperable'
FROM CREDITO.PlanPago pp
INNER JOIN CREDITO.Credito c ON c.CreditoId = pp.CreditoId
WHERE pp.Estado = 'PEN' AND c.Estado = 'DES' AND c.FechaVencimiento < @Hoy
AND c.CreditoId=ISNULL(@CreditoId,c.CreditoId)
