
-- CREDITO.usp_RptCreditosMorososPagados 8,1,'20200701','20200730'

CREATE PROC [CREDITO].[usp_RptCreditosMorososPagados]
@UsuarioId INT = NULL,
@OficinaId INT = NULL,
@FechaInicio Date ,
@FechaFin Date 
AS

SELECT	C.CreditoId,P.NombreCompleto 'Cliente',C.MontoCredito,C.Interes,C.FormaPago,C.NumeroCuotas,
		C.MontoGastosAdm,C.CentralRiesgo,C.FechaPrimerPago,C.FechaVencimiento,C.FechaPagado,
		A.NombreCompleto 'Agente'
FROM CREDITO.Credito C
INNER JOIN MAESTRO.Persona P on C.PersonaId=P.PersonaId
INNER JOIN MAESTRO.Usuario U on C.UsuarioRegId=U.UsuarioId
INNER JOIN MAESTRO.Persona A on U.PersonaId=A.PersonaId
WHERE	C.OficinaId = ISNULL(@OficinaId,C.OficinaId)  AND
		C.UsuarioRegId = ISNULL(@UsuarioId,C.UsuarioRegId) AND
		C.Estado='PAG'AND CAST(C.FechaPagado AS DATE)>C.FechaVencimiento AND
		CAST(C.FechaPagado AS DATE) BETWEEN @FechaInicio AND @FechaFin
