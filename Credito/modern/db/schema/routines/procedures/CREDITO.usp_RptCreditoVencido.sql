 /*
 CREDITO.usp_RptCreditoVencido @VencidoMayor60='S'
 CREDITO.uspCreditoVencido
 */
 
CREATE PROC [CREDITO].[usp_RptCreditoVencido]
@VencidoMenor60 CHAR(1)=NULL,
@VencidoMayor60 CHAR(1)=NULL,
@VencidoIrrecuperable CHAR(1)=NULL
AS

DECLARE @AgenteIrrecuperable INT = (SELECT UsuarioId FROM MAESTRO.Usuario WHERE NombreUsuario='IRRECUPERABLE')


DECLARE @Hoy DATE = dbo.ufnFecha()
;WITH VENCIDO AS(
	SELECT c.CreditoId,
		SUM(pp.Cuota - pp.PagoLibre) 'CreditoVencido'
	FROM CREDITO.PlanPago pp
	INNER JOIN CREDITO.Credito c ON c.CreditoId = pp.CreditoId
	WHERE pp.Estado = 'PEN' AND c.Estado = 'DES' AND c.FechaVencimiento < @Hoy 
	GROUP BY c.CreditoId
),
VENCIDODETALLE AS (
	SELECT v.CreditoId,v.CreditoVencido,
		CASE WHEN c.FechaVencimiento BETWEEN DATEADD(DAY, -60, @Hoy) AND @Hoy THEN 'S' ELSE 'N' END 'VencidoMenor60',
	    CASE WHEN c.FechaVencimiento < DATEADD(DAY, -60, @Hoy)  THEN 'S' ELSE 'N' END 'VencidoMayor60',
		CASE WHEN c.UsuarioRegId = @AgenteIrrecuperable   THEN 'S' ELSE 'N' END 'VencidoIrrecuperable'
	FROM VENCIDO V
	INNER JOIN CREDITO.Credito c ON c.CreditoId = V.CreditoId
)
SELECT G.NombreCompleto 'Gestor', C.CreditoId,p.NombreCompleto 'Cliente',C.MontoCredito,c.FormaPago,C.FechaVencimiento, V.CreditoVencido, 
		VencidoMenor60,VencidoMayor60,VencidoIrrecuperable
FROM VENCIDODETALLE V
INNER JOIN CREDITO.Credito C ON C.CreditoId = V.CreditoId
INNER JOIN MAESTRO.Persona P ON P.PersonaId = C.PersonaId
INNER JOIN MAESTRO.Usuario U ON U.UsuarioId = C.UsuarioRegId
INNER JOIN MAESTRO.Persona G ON G.PersonaId = U.PersonaId
WHERE VencidoMenor60=ISNULL(@VencidoMenor60,VencidoMenor60) 
	AND VencidoMayor60=ISNULL(@VencidoMayor60,VencidoMayor60) 
	AND VencidoIrrecuperable= ISNULL(@VencidoIrrecuperable,VencidoIrrecuperable)
ORDER BY Gestor, Cliente
