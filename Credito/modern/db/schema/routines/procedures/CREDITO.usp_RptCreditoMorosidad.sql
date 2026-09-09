/*
SELECT * FROM CREDITO.PlanPago WHERE CreditoId=45
[CREDITO].[usp_RptCreditoMorosidad] NULL,'20140601', 1,1000
*/
CREATE PROC [CREDITO].[usp_RptCreditoMorosidad]
@OficinaId INT = NULL,
@HastaFecha DATE  =NULL,
@DiasAtrazoIni INT,
@DiasAtrazoFin INT
AS

DECLARE @FechaAct DATE=dbo.ufnFecha() 
DECLARE @tblCreditoMora TABLE(id INT IDENTITY(1,1),CreditoId INT,CuotasAtrazo INT,CapitalAtrazo DECIMAL(16,2),GA DECIMAL(16,2),
							InteresAtrazo DECIMAL(16,2),Mora DECIMAL(16,2),ImporteLibre DECIMAL(16,2),DiasAtrazo INT,DeudaAtrazo DECIMAL(16,2))
DECLARE @tCuotasPendientes TABLE(Id INT IDENTITY(1,1),PlanPagoId INT,Glosa VARCHAR(MAX),FechaVencimiento DATE,Amortizacion DECIMAL(16,2),
								Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2), Cuota DECIMAL(16,2), DiasAtrazo INT,
								ImporteMora DECIMAL(16,2),Descuento DECIMAL(16,2), Cargo DECIMAL(16,2),
								PagoLibre DECIMAL(16,2),PagoCuota DECIMAL(16,2))

;WITH DESEMBOLSOS AS(
	SELECT	C.CreditoId,
			dbo.ufnCalcularDiasAtrazo(
				(SELECT MIN(FechaVencimiento) FROM CREDITO.PlanPago WITH(NOLOCK) WHERE CreditoId=C.CreditoId AND Estado='PEN')
				,@FechaAct) 'DiasAtrazo'			
	FROM	CREDITO.Credito C WITH(NOLOCK)
	WHERE	C.Estado='DES' AND C.OficinaId=ISNULL(@OficinaId,C.OficinaId) AND
			CAST(C.FechaDesembolso AS DATE) <= ISNULL(@HastaFecha, @FechaAct)
)
INSERT INTO @tblCreditoMora(CreditoId,DiasAtrazo)
SELECT	C.CreditoId,CM.DiasAtrazo
FROM CREDITO.Credito C WITH(NOLOCK)
INNER JOIN DESEMBOLSOS CM ON C.CreditoId = CM.CreditoId
WHERE CM.DiasAtrazo BETWEEN @DiasAtrazoIni AND @DiasAtrazoFin	

DECLARE @index INT=1,@Filas INT = (SELECT COUNT(1) FROM @tblCreditoMora),@CreditoId INT
WHILE(@index<=@Filas)
BEGIN
	SELECT @CreditoId=CreditoId FROM @tblCreditoMora WHERE id=@index
	
	DELETE FROM @tCuotasPendientes
	INSERT INTO @tCuotasPendientes
	EXEC CREDITO.usp_CuotasPendientes @CreditoId,@FechaAct
	
	UPDATE @tblCreditoMora 
	SET CapitalAtrazo = (SELECT SUM(Amortizacion) FROM @tCuotasPendientes WHERE DiasAtrazo>0),
	GA = (SELECT SUM(GastosAdm) FROM @tCuotasPendientes WHERE DiasAtrazo>0),
	InteresAtrazo = (SELECT SUM(Interes) FROM @tCuotasPendientes WHERE DiasAtrazo>0),
	Mora = (SELECT SUM(ImporteMora) FROM @tCuotasPendientes WHERE DiasAtrazo>0),
	ImporteLibre = (SELECT SUM(PagoLibre) FROM @tCuotasPendientes WHERE DiasAtrazo>0),
	DiasAtrazo = (SELECT MAX(DiasAtrazo) FROM @tCuotasPendientes WHERE DiasAtrazo>0),
	CuotasAtrazo = (SELECT COUNT(1) FROM @tCuotasPendientes WHERE DiasAtrazo>0),
	DeudaAtrazo = (SELECT SUM(PagoCuota) FROM @tCuotasPendientes WHERE DiasAtrazo>0)
	WHERE CreditoId=@CreditoId
	
	SET @index=@index+1
END


SELECT	C.CreditoId,P.NombreCompleto 'Cliente',p.Direccion,p.Celular1 'Celular',
		FechaDesembolso,(SELECT MAX(FechaVencimiento) FROM CREDITO.PlanPago WITH(NOLOCK) WHERE CreditoId=C.CreditoId)'FechaVcto',
		Descripcion 'Articulo',MontoCredito,
		(SELECT SUM(Amortizacion) FROM CREDITO.PlanPago WITH(NOLOCK) WHERE CreditoId=C.CreditoId AND Estado='PEN') 'SaldoCredito' ,
		(SELECT MAX(FechaPagoCuota) FROM CREDITO.PlanPago WITH(NOLOCK) WHERE CreditoId=C.CreditoId AND Estado='PAG') 'FechaUltPago',
		CR.CapitalAtrazo,CR.GA,CR.InteresAtrazo,CR.Mora,CR.ImporteLibre,
		CR.DiasAtrazo,CR.CuotasAtrazo,CR.DeudaAtrazo
FROM CREDITO.Credito C WITH(NOLOCK)
INNER JOIN @tblCreditoMora CR ON C.CreditoId = CR.CreditoId
INNER JOIN MAESTRO.Persona P WITH(NOLOCK) ON C.PersonaId = P.PersonaId
ORDER BY P.NombreCompleto
