-- exec CREDITO.usp_RptCreditosCierres '20200701','20200730',8

CREATE PROC [CREDITO].[usp_RptCreditosCierres]
@FechaIni DATE ,
@FechaFin DATE ,
@UsuarioId INT = NULL ,
@OficinaId INT = NULL 
AS
;WITH CREDITO_FECHA AS(
	SELECT PP.CreditoId, SUM(ISNULL(PP.Amortizacion,0)) 'SumAmortizacion',SUM(ISNULL(PP.Interes,0)) 'SumInteres',
	SUM(ISNULL(PP.Cuota,0)) 'SumCuota',MAX(PP.Numero) 'SumNroCuota'
	FROM CREDITO.PlanPago PP
	INNER JOIN CREDITO.Credito C ON C.CreditoId=PP.CreditoId
	WHERE PP.CreditoId=C.CreditoId AND PP.Estado='PAG' AND CAST(C.FechaPrimerPago AS DATE) BETWEEN @FechaIni AND @FechaFin
	AND C.OficinaId=ISNULL(@OficinaId,C.OficinaId) AND C.UsuarioRegId=ISNULL(@UsuarioId,C.UsuarioRegId) 
	AND C.Estado IN('PAG','DES')
	GROUP BY PP.CreditoId
)
SELECT	CF.CreditoId, 
		CASE C.Estado WHEN 'DES' THEN 'DESEMBOLSADO' WHEN 'PAG' THEN 'PAGADO' END 'Estado',
		A.NombreCompleto 'Agente',P.Codigo,P.NombreCompleto 'Cliente',
		C.MontoCredito,C.FormaPago,C.NumeroCuotas,C.Interes,C.MontoGastosAdm,C.CentralRiesgo,
		C.FechaPrimerPago,C.FechaVencimiento,
		SumAmortizacion,SumInteres,SumCuota,SumNroCuota
FROM CREDITO_FECHA CF
INNER JOIN CREDITO.Credito C ON C.CreditoId=CF.CreditoId
INNER JOIN MAESTRO.Persona P ON C.PersonaId=P.PersonaId
INNER JOIN MAESTRO.Usuario u ON C.UsuarioRegId = u.UsuarioId
INNER JOIN MAESTRO.Persona A ON u.PersonaId=A.PersonaId	
ORDER BY Estado,Agente
