
-- CREDITO.usp_RptCreditoRentabilidad 1,'20200301','20200330'
CREATE PROC [CREDITO].[usp_RptCreditoRentabilidad]
@OficnaId INT = NULL,
@FechaIni DATE ,
@FechaFin DATE,
@EstadoCredito char(3) 
AS

;WITH CREDITOSUM AS(
	SELECT CR.CreditoId,COUNT(1) 'CuotasPagadas', SUM(PP.Interes) 'SumInteres',
	SUM(PP.Cuota) 'SumCuota',SUM(PP.ImporteMora) 'SumMora',
	SUM(PP.PagoCuota + PP.PagoLibre) 'SumPago',MAX(PP.FechaPagoCuota) 'FechaPago'
	FROM CREDITO.Credito CR
	INNER JOIN CREDITO.PlanPago PP ON CR.CreditoId = PP.CreditoId AND PP.Estado='PAG'
	WHERE CR.Estado=@EstadoCredito /* in('PAG','REP')*/  AND	CR.OficinaId=ISNULL(@OficnaId,CR.OficinaId)		
	GROUP BY CR.CreditoId
)
SELECT	C.CreditoId,O.Denominacion 'Oficina',P.Codigo,P.NombreCompleto 'Cliente',
		C.FechaDesembolso,CS.FechaPago,C.NumeroCuotas,C.FormaPago,c.Estado,
		C.MontoCredito,C.Interes,CS.SumCuota,CS.CuotasPagadas,C.MontoGastosAdm,
		CS.SumInteres,CS.SumMora,CS.SumPago
FROM CREDITO.Credito C
INNER JOIN CREDITOSUM CS ON C.CreditoId = CS.CreditoId
INNER JOIN MAESTRO.Persona P ON C.PersonaId = P.PersonaId
INNER JOIN MAESTRO.Oficina O ON C.OficinaId = O.OficinaId
WHERE CAST(CS.FechaPago AS DATE) BETWEEN @FechaIni AND @FechaFin
ORDER BY CS.FechaPago
