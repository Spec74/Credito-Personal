-- CREDITO.usp_RegenerarPlanPago 56373
CREATE PROC [CREDITO].[usp_RegenerarPlanPago]
@CreditoId INT
AS

DECLARE @TotalPagoCaja DECIMAL(16,2) = ISNULL((SELECT  SUM(ImportePago) FROM  CREDITO.MovimientoCaja 
										WHERE CreditoId=@CreditoId AND Operacion='CUO' AND ImportePago>0 AND Estado=1),0)
DECLARE @NroCuotas INT ,@FormaPago CHAR(1)

SELECT @NroCuotas=NumeroCuotas, @FormaPago=FormaPago FROM CREDITO.Credito WHERE CreditoId=@CreditoId

;WITH CuotasConSaldoBase AS (
    SELECT 
		PlanPagoId,
        Numero,
		PagoCuota,
		--IIF(@FormaPago='M',Cuota + Cargo + ImporteMora,Cuota + Cargo)  'ImporteCuota'
		Cuota + Cargo  'ImporteCuota'
    FROM CREDITO.PlanPago 
    WHERE CreditoId = @CreditoId
),
CuotasConSaldo AS (
    SELECT 
		PlanPagoId,
        Numero, 
		ImporteCuota,
		SUM( IIF(Numero=@NroCuotas,ISNULL(PagoCuota,ImporteCuota),ImporteCuota)    ) OVER (ORDER BY Numero) AS SaldoAcumulado
    FROM CuotasConSaldoBase
), PagoCuota AS	(
	SELECT *, 
		CASE WHEN cs.Numero = (SELECT MIN(Numero) FROM CuotasConSaldo WHERE SaldoAcumulado > @TotalPagoCaja) 
			THEN @TotalPagoCaja - COALESCE(LAG(SaldoAcumulado) OVER (ORDER BY Numero), 0) 
		ELSE 0 END 'PagoLibre'
	FROM  CuotasConSaldo cs
)
UPDATE pp
SET Estado = CASE WHEN cs.SaldoAcumulado <= @TotalPagoCaja THEN 'PAG' ELSE 'PEN' END,
	PagoCuota = cs.ImporteCuota,
    PagoLibre = cs.PagoLibre
FROM CREDITO.PlanPago pp
INNER JOIN PagoCuota cs ON pp.PlanPagoId = cs.PlanPagoId 

UPDATE CREDITO.Credito SET 
    Estado = CASE WHEN EXISTS (SELECT 1 FROM CREDITO.PlanPago WHERE CreditoId = @CreditoId AND Estado = 'PEN') THEN 'DES' ELSE 'PAG' END,
	FechaMod = dbo.ufnFecha()
WHERE CreditoId = @CreditoId

UPDATE C SET C.FechaPagado=pp.FechaPagoCuota
--SELECT pp.* FROM 
FROM CREDITO.Credito C
INNER JOIN CREDITO.PlanPago pp ON pp.CreditoId = C.CreditoId AND pp.Numero=C.NumeroCuotas
WHERE C.CreditoId = @CreditoId 
AND C.Estado='PAG'
