
/*

EXEC CREDITO.[usp_PagarCuotaPagoLibre] 18380,55557,3.96,8

*/
CREATE PROC [CREDITO].[usp_PagarCuotaPagoLibre]
@CajaDiarioId INT ,
@CreditoId INT,
@ImporteRecibido DECIMAL(16,2) = 0,
@UsuarioId INT,
@TipoPagoId INT=1,
@FechaPagoTransferencia VARCHAR(16)=''
AS

	DECLARE @TotalPago DECIMAL(16,2)=0, @IndImporteLibre BIT = 0
	DECLARE @FechaActual DATETIME=dbo.ufnFecha()

	DECLARE @PersonaId INT, @MovimientoCajaId INT
	SELECT	@PersonaId = PersonaId FROM	CREDITO.Credito WHERE CreditoId = @CreditoId


	DECLARE @tCuotasPendientes TABLE(Id INT IDENTITY(1,1),PlanPagoId INT,Glosa VARCHAR(MAX),FechaVencimiento DATE,Amortizacion DECIMAL(16,2),
									Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2), Cuota DECIMAL(16,2), DiasAtrazo INT,
									ImporteMora DECIMAL(16,2),Descuento DECIMAL(16,2), Cargo DECIMAL(16,2),
									PagoLibre DECIMAL(16,2),PagoCuota DECIMAL(16,2))
	INSERT INTO @tCuotasPendientes
	EXEC CREDITO.usp_CuotasPendientes @CreditoId,@FechaActual
	--SELECT * FROM @tCuotasPendientes

	
	-- PAGO IMPORTE LIBRE
	INSERT INTO CREDITO.MovimientoCaja
			( CajaDiarioId,PersonaId ,Operacion ,ImportePago ,
			  Descripcion ,IndEntrada ,Estado,OrdenVentaId,CreditoId ,UsuarioRegId ,FechaReg,TipoPagoId)
	VALUES  ( @CajaDiarioId,@PersonaId, 'CUO' , @ImporteRecibido , 
			  'CREDITO ' + CAST(@CreditoId AS VARCHAR(20)) + ' PAGO LIBRE ' + CAST(@ImporteRecibido AS VARCHAR(20)) , 
			  1, 1,NULL,@CreditoId, @UsuarioId , @FechaActual,@TipoPagoId)
	SET @MovimientoCajaId = @@IDENTITY
	
	
	IF @TipoPagoId>1 -- SOLO PAGOS POR YAPE, PLIN, BCP, NACION
		INSERT INTO	CREDITO.MovimientoCajaExtension
		(	MovimientoCajaId, FechaTransferencia, IndTransferenciaVerificada )
		VALUES	(   @MovimientoCajaId, @FechaPagoTransferencia, 0 )
	
	UPDATE PP
	SET PP.DiasAtrazo= CP.DiasAtrazo ,
		PP.ImporteMora=CP.ImporteMora,
		PP.PagoCuota = CP.PagoCuota + cp.PagoLibre,		
		PP.MovimientoCajaId=@MovimientoCajaId,
		PP.FechaPagoCuota=@FechaActual,
		PP.UsuarioModId=@UsuarioId,
		PP.FechaMod=@FechaActual
	FROM @tCuotasPendientes CP 
	INNER JOIN CREDITO.PlanPago PP ON CP.PlanPagoId = PP.PlanPagoId
	WHERE PP.CreditoId=@CreditoId
	
	
	EXEC CREDITO.usp_RegenerarPlanPago @CreditoId = @CreditoId 
			
	EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId = @CajaDiarioId 
	
	SELECT @MovimientoCajaId
