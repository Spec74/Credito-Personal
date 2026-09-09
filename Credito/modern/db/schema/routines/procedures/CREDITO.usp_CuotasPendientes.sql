
/* 
	SELECT * FROM CREDITO.Credito where estado='DES'
	SELECT * FROM CREDITO.PlanPago where creditoid=88
	CREDITO.usp_CuotasPendientes 54539,'20241202',0	
*/
CREATE PROC [CREDITO].[usp_CuotasPendientes]
@CreditoId INT,
@FechaCalculo DATE,
@IndCancelacion BIT = 0
AS

DECLARE @CuotaCalculo INT,@CuotaIni INT,@CuotaFin INT,@CuotaCancel INT,@Modalidad CHAR(1), @FechaVctoIni DATE
DECLARE @tplanpago TABLE(PlanPagoId INT,Glosa VARCHAR(MAX),FechaVencimiento DATE,Amortizacion DECIMAL(16,2),
						Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2), Cuota DECIMAL(16,2), DiasAtrazo INT,
						ImporteMora DECIMAL(16,2),Descuento DECIMAL(16,2), Cargo DECIMAL(16,2),
						PagoLibre DECIMAL(16,2),PagoCuota DECIMAL(16,2))

IF @IndCancelacion=1
BEGIN
	SELECT	TOP 1 @CuotaCalculo=Numero 
	FROM	CREDITO.PlanPago PP WITH(NOLOCK)
	WHERE	PP.CreditoId=@CreditoId AND PP.Estado='PEN' AND @FechaCalculo<=PP.FechaVencimiento 
	ORDER BY Numero

	IF @CuotaCalculo IS NULL
		SELECT	TOP 1 @CuotaCalculo=MAX(Numero) 
		FROM	CREDITO.PlanPago PP WITH(NOLOCK)
		WHERE	PP.CreditoId=@CreditoId AND PP.Estado='PEN' 

	SELECT @CuotaFin = CASE FormaPago 
								WHEN 'M' THEN  @CuotaCalculo
								WHEN 'Q' THEN  CEILING(CAST(@CuotaCalculo AS DECIMAL)/2) * 2
								WHEN 'S' THEN  CEILING(CAST(@CuotaCalculo AS DECIMAL)/4) * 4
								WHEN 'D' THEN  CEILING(CAST(@CuotaCalculo AS DECIMAL)/26) * 26
						 END,
			@CuotaIni = CASE FormaPago 
								WHEN 'M' THEN  @CuotaCalculo
								WHEN 'Q' THEN  CEILING(CAST(@CuotaCalculo AS DECIMAL)/2) * 2 - 1
								WHEN 'S' THEN  CEILING(CAST(@CuotaCalculo AS DECIMAL)/4) * 4 - 3
								WHEN 'D' THEN  CEILING(CAST(@CuotaCalculo AS DECIMAL)/26) * 26 - 25
						 END,
			@Modalidad=FormaPago
	FROM CREDITO.Credito WITH(NOLOCK)
	WHERE CreditoId=@CreditoId

	SET @CuotaCancel=@CuotaFin
	
	IF @Modalidad='D'
	BEGIN
		SELECT @FechaVctoIni=FechaVencimiento FROM CREDITO.PlanPago WITH(NOLOCK)
		WHERE CreditoId=@CreditoId AND Numero=@CuotaIni
		IF DATEDIFF(DAY,@FechaVctoIni,@FechaCalculo)<2
			SET @CuotaCancel=@CuotaCalculo
	END
	ELSE IF @Modalidad='M'
	BEGIN
		IF @CuotaCalculo>1
			SELECT @FechaVctoIni=FechaVencimiento FROM CREDITO.PlanPago WITH(NOLOCK)
			WHERE CreditoId=@CreditoId AND Numero=@CuotaCalculo-1
		ELSE
			SELECT @FechaVctoIni=DATEADD(MONTH,-1,FechaVencimiento) FROM CREDITO.PlanPago WITH(NOLOCK)
			WHERE CreditoId=@CreditoId AND Numero=1
			
		SET @FechaVctoIni=DATEADD(DAY,1,@FechaVctoIni)
		--SELECT @FechaVctoIni 'VctoIni',@FechaCalculo 'FCalculo',DATEDIFF(DAY,@FechaVctoIni,@FechaCalculo)
		IF DATEDIFF(DAY,@FechaVctoIni,@FechaCalculo)<2
			SET @CuotaCancel=@CuotaCalculo - 1		
	END
	ELSE
	BEGIN		
		IF @CuotaCalculo = @CuotaIni
			BEGIN
				IF @CuotaCalculo>1
					SELECT @FechaVctoIni=FechaVencimiento FROM CREDITO.PlanPago WITH(NOLOCK)
					WHERE CreditoId=@CreditoId AND Numero=@CuotaCalculo-1
				ELSE
				BEGIN
					IF @Modalidad='Q'
						SELECT @FechaVctoIni=DATEADD(day,-15,FechaVencimiento) FROM CREDITO.PlanPago WITH(NOLOCK)
						WHERE CreditoId=@CreditoId AND Numero=1
					IF @Modalidad='S'
						SELECT @FechaVctoIni=DATEADD(day,-7,FechaVencimiento) FROM CREDITO.PlanPago WITH(NOLOCK)
						WHERE CreditoId=@CreditoId AND Numero=1
				END	
				SET @FechaVctoIni=DATEADD(DAY,1,@FechaVctoIni)
				--SELECT @FechaVctoIni 'VctoIni',@FechaCalculo 'FCalculo',DATEDIFF(DAY,@FechaVctoIni,@FechaCalculo)
				IF DATEDIFF(DAY,@FechaVctoIni,@FechaCalculo)<0
					SET @CuotaCancel=0
				ELSE IF DATEDIFF(DAY,@FechaVctoIni,@FechaCalculo)<2
					SET @CuotaCancel=@CuotaCalculo
			END	
	END
	--SELECT @CuotaIni 'CuotaIni',@CuotaCalculo 'CuotaCalculo',@CuotaFin 'CuotaFin', @CuotaCancel 'CuotaCancelacion'
END

;WITH CUOTAS AS(
	SELECT	pp.PlanPagoId, 
			'CUOTA ' + CAST(PP.Numero AS VARCHAR(8)) 'Glosa', 
			PP.FechaVencimiento,PP.Amortizacion,PP.Interes,PP.GastosAdm, PP.Cuota ,
			0 'DiasAtrazo',pp.ImporteMora,
			ISNULL(PP.PagoLibre,0) 'PagoLibre',C.FechaDesembolso,PP.Descuento,PP.Cargo
	FROM CREDITO.Credito C WITH(NOLOCK)
	INNER JOIN CREDITO.PlanPago PP WITH(NOLOCK) ON C.CreditoId = PP.CreditoId AND PP.Estado='PEN'
	LEFT JOIN CREDITO.Producto P WITH(NOLOCK) ON C.ProductoId=P.ProductoId
	WHERE C.Estado='DES' AND C.CreditoId=@CreditoId
)
INSERT INTO @tplanpago
	SELECT	PlanPagoId, Glosa, FechaVencimiento, Amortizacion,Interes,GastosAdm,Cuota, 
			DiasAtrazo, ImporteMora, Descuento, Cargo, PagoLibre, 
			0.0 'PagoCuota'
	FROM CUOTAS 	
ORDER BY 1

/*cambio importe moratorio 1% 1 sol por cada 100*/

DECLARE @planPagoId INT = (SELECT TOP 1 PlanPagoId FROM @tplanpago order by FechaVencimiento ASC)
DECLARE @MovCaja DECIMAL(15,2) = (SELECT SUM(ImportePago) FROM CREDITO.MovimientoCaja WITH(NOLOCK) WHERE CreditoId=@CreditoId and ImportePago>0 and Operacion='CUO' and Estado=1)
DECLARE @MontoTotalCredito DECIMAL(15,2) = (SELECT SUM(Cuota) FROM CREDITO.PlanPago WITH(NOLOCK) WHERE CreditoId=@CreditoId ) 
DECLARE @ImportePendiente DECIMAL(15,2) = (@MontoTotalCredito - ISNULL(@MovCaja,0))
IF @ImportePendiente<0
	SET @ImportePendiente=0

SELECT @Modalidad=FormaPago FROM CREDITO.Credito WITH(NOLOCK) WHERE CreditoId=@CreditoId	
IF @Modalidad='M'
BEGIN    
	WITH VENCIMIENTO_MES AS(
		SELECT PlanPagoId,Numero, dbo.ufnCalcularDiasAtrazo(FechaVencimiento,@FechaCalculo) 'DiasAtrazoMes' 
		FROM CREDITO.PlanPago
		WHERE CreditoId= @CreditoId AND Estado='PEN'
	)
	UPDATE PP
	SET PP.ImporteMora=dbo.ufnCalcularMora(Cuota,VM.DiasAtrazoMes,0) ,
		pp.DiasAtrazo=VM.DiasAtrazoMes		
	--SELECT VM.* 
	FROM VENCIMIENTO_MES VM
	INNER JOIN @tplanpago PP ON PP.PlanPagoId = VM.PlanPagoId

	--UPDATE @tplanpago SET PagoCuota=Cuota + Cargo - PagoLibre + ImporteMora -- - Descuento 
	--UPDATE	@tplanpago 
	--SET		ImporteMora= (Interes /30) * DiasAtrazo   ,
	--		PagoCuota = IIF(PagoCuota>=0,PagoCuota + ((Interes /30) * DiasAtrazo) ,0) 	
END
ELSE
BEGIN
	DECLARE @DiasAtrazoUltimaCuota INT = (SELECT TOP 1 dbo.ufnCalcularDiasAtrazo(FechaVencimiento,@FechaCalculo) 
											FROM CREDITO.PlanPago WHERE CreditoId=@CreditoId ORDER BY Numero DESC)

    --DECLARE @DiasAtrazoUltimaCuota INT = (SELECT TOP 1 DiasAtrazo FROM @tplanpago order by FechaVencimiento DESC)
    UPDATE	@tplanpago 
	SET		ImporteMora= dbo.ufnCalcularMora( Cuota , @DiasAtrazoUltimaCuota, 0), 
			DiasAtrazo = @DiasAtrazoUltimaCuota	

	
END

UPDATE @tplanpago SET PagoCuota=Cuota + Cargo - PagoLibre --+ ImporteMora - Descuento 

DECLARE @PlanPagoIdUltimaCuota INT = (SELECT TOP 1 PlanPagoId FROM @tplanpago order by FechaVencimiento DESC)
DECLARE @SumaMoraPendiente DECIMAL(15,2) = (SELECT SUM(ImporteMora) FROM @tplanpago)
DECLARE @SumaMoraAnterior DECIMAL(15,2) = (SELECT SUM(ImporteMora) FROM CREDITO.PlanPago WHERE CreditoId=@CreditoId AND Estado='PAG')

UPDATE @tplanpago SET PagoCuota=PagoCuota + ISNULL(@SumaMoraPendiente,0) + ISNULL(@SumaMoraAnterior,0)
WHERE PlanPagoId=@PlanPagoIdUltimaCuota

select * FROM @tplanpago
