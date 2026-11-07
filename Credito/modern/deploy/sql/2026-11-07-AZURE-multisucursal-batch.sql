/*
================================================================================
  AZURE — batch multisucursal 2026-11-07
  Ejecutar en SSMS / Azure Data Studio contra la BD Credito.
  Preferible: sqlcmd -I (QUOTED_IDENTIFIER ON).

  Incluye guards de oficina en cobros, CompletarImpagos, TransferirBoveda,
  reportes CreditosActivos / Rentabilidad / SaldosCajaResumenTipoCuenta.
  Idempotente (CREATE OR ALTER). Este único archivo basta.
================================================================================
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO


/* ---- CREDITO.usp_PagarCuotas ---- */


/*
UPDATE CREDITO.PlanPago SET Estado='PEN', MovimientoCajaId=NULL,FechaPagoCuota=NULL, DiasAtrazo=0,Mora=0,PagoCuota=0
WHERE PlanPagoId IN(2)
DELETE FROM CREDITO.MovimientoCaja

EXEC CREDITO.usp_PagarCuotas 2,'2',100,3

SELECT * FROM CREDITO.PlanPago where planpagoid in(2)
SELECT * FROM CREDITO.MovimientoCaja
SELECT * FROM VENTAS.OrdenVenta

*/
CREATE OR ALTER PROC [CREDITO].[usp_PagarCuotas]
@CajaDiarioId INT ,
@CreditoId INT,
@ListaPlanPagoId VARCHAR(MAX),
@ImporteRecibido DECIMAL(16,2) = 0,
@UsuarioId INT,
@FechaPago DATE=NULL,
@TipoPagoId INT=1,
@FechaPagoTransferencia VARCHAR(16)=''
AS

DECLARE @TotalPago DECIMAL(16,2)=0, @IndImporteLibre BIT = 0
DECLARE @FechaActual Datetime=dbo.ufnFecha()
DECLARE @OficinaCredito INT, @OficinaCaja INT

-- Multisucursal: el cobro solo puede ir a la caja de la misma oficina del crédito.
SELECT @OficinaCredito = OficinaId FROM CREDITO.Credito WHERE CreditoId = @CreditoId
SELECT @OficinaCaja = cj.OficinaId
FROM CREDITO.CajaDiario cd
INNER JOIN CREDITO.Caja cj ON cj.CajaId = cd.CajaId
WHERE cd.CajaDiarioId = @CajaDiarioId

IF @OficinaCredito IS NULL
BEGIN
	RAISERROR(N'Crédito no existe.', 16, 1)
	RETURN
END
IF @OficinaCaja IS NULL
BEGIN
	RAISERROR(N'Caja diario no existe.', 16, 1)
	RETURN
END
IF @OficinaCredito <> @OficinaCaja
BEGIN
	RAISERROR(N'El crédito no pertenece a la oficina de la caja (multisucursal).', 16, 1)
	RETURN
END

IF @ListaPlanPagoId IS NULL
	SET @ListaPlanPagoId = ''
	
IF LEN(@ListaPlanPagoId) > 0 AND NOT EXISTS(	SELECT	1 FROM dbo.Split(@ListaPlanPagoId,',') L
												INNER JOIN CREDITO.PlanPago PP ON L.Name=pp.PlanPagoId
												WHERE PP.Estado='PEN' AND PP.CreditoId=@CreditoId)
BEGIN
	RETURN
END

IF @FechaPago IS NULL
	SET @FechaPago=@FechaActual

DECLARE @tCuotasPendientes TABLE(Id INT IDENTITY(1,1),PlanPagoId INT,Glosa VARCHAR(MAX),FechaVencimiento DATE,Amortizacion DECIMAL(16,2),
								Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2), Cuota DECIMAL(16,2), DiasAtrazo INT,
								ImporteMora DECIMAL(16,2),Descuento DECIMAL(16,2), Cargo DECIMAL(16,2),
								PagoLibre DECIMAL(16,2),PagoCuota DECIMAL(16,2))
INSERT INTO @tCuotasPendientes
EXEC CREDITO.usp_CuotasPendientes @CreditoId,@FechaPago
--SELECT * FROM @tCuotasPendientes

DECLARE @PlanPagoIdUlt INT,@SumaPagoCuotaUlt DECIMAL(16,2)=0
IF LEN(@ListaPlanPagoId) = 0
BEGIN
	DECLARE @NroCuotas INT=0,@PagoCuota DECIMAL(16,2)=0,@Index INT=1
	SET @NroCuotas = (SELECT COUNT(1) FROM @tCuotasPendientes)
	SET @PagoCuota = (SELECT TOP 1 PagoCuota FROM @tCuotasPendientes)
	SET @IndImporteLibre = 1
	
	WHILE @SumaPagoCuotaUlt <= @ImporteRecibido AND @Index <= @NroCuotas
	BEGIN
		SELECT @PagoCuota=PagoCuota, @PlanPagoIdUlt=PlanPagoId 
		FROM @tCuotasPendientes WHERE Id=@Index
		
		IF @SumaPagoCuotaUlt + @PagoCuota <= @ImporteRecibido
		BEGIN
			SET @SumaPagoCuotaUlt = @SumaPagoCuotaUlt + @PagoCuota
			SET @ListaPlanPagoId = @ListaPlanPagoId + CAST(@PlanPagoIdUlt AS VARCHAR(10)) + ','
		END
		ELSE
			BREAK
		SET @Index = @Index + 1
	END
	
	IF LEN(@ListaPlanPagoId)>0
		SET @ListaPlanPagoId = SUBSTRING(@ListaPlanPagoId,1,LEN(@ListaPlanPagoId)-1)
END
--SELECT @ListaPlanPagoId
UPDATE PP
SET DiasAtrazo= CP.DiasAtrazo ,
	ImporteMora=CP.ImporteMora,
	PagoCuota = CP.PagoCuota
FROM dbo.Split(@ListaPlanPagoId,',') L 
INNER JOIN @tCuotasPendientes CP ON L.Name=CP.PlanPagoId
INNER JOIN CREDITO.PlanPago PP WITH(NOLOCK) ON CP.PlanPagoId = PP.PlanPagoId
WHERE PP.CreditoId=@CreditoId

SELECT	@TotalPago = SUM(PP.PagoCuota) 
FROM dbo.Split(@ListaPlanPagoId,',') L
INNER JOIN CREDITO.PlanPago PP WITH(NOLOCK) ON L.Name=pp.PlanPagoId
WHERE PP.Estado='PEN' AND PP.CreditoId=@CreditoId

DECLARE @Cuotas VARCHAR(MAX)='',@PersonaId INT, @OrdenVentaId INT
SELECT	@PersonaId = PersonaId , @OrdenVentaId = OrdenVentaId
FROM	CREDITO.Credito WITH(NOLOCK) WHERE CreditoId = @CreditoId

SET @Cuotas = STUFF((SELECT ',' + rtrim(convert(char(15),Numero))
				FROM dbo.Split(@ListaPlanPagoId,',') L
				INNER JOIN CREDITO.PlanPago PP WITH(NOLOCK) ON L.Name=pp.PlanPagoId
				FOR XML PATH('')),1,1,'') 
	
DECLARE @MovimientoCajaId INT
IF @IndImporteLibre=0
BEGIN
	INSERT INTO CREDITO.MovimientoCaja
			( CajaDiarioId,PersonaId ,Operacion ,ImportePago ,
			  Descripcion ,IndEntrada ,Estado,OrdenVentaId,CreditoId,UsuarioRegId ,FechaReg,TipoPagoId
			)
	VALUES  ( @CajaDiarioId,@PersonaId, 'CUO' , @TotalPago , 
			  'CREDITO ' + CAST(@CreditoId AS VARCHAR(20)) + ' CUOTA ' + @Cuotas  , 
			  1, 1,@OrdenVentaId,@CreditoId, @UsuarioId , @FechaActual,@TipoPagoId)
	SET @MovimientoCajaId = @@IDENTITY
END			  
ELSE
BEGIN
	-- PAGO IMPORTE LIBRE
	INSERT INTO CREDITO.MovimientoCaja
			( CajaDiarioId,PersonaId ,Operacion ,ImportePago ,
			  Descripcion ,IndEntrada ,Estado,OrdenVentaId,CreditoId ,UsuarioRegId ,FechaReg,TipoPagoId
			)
	VALUES  ( @CajaDiarioId,@PersonaId, 'CUO' , @ImporteRecibido , 
			  'CREDITO ' + CAST(@CreditoId AS VARCHAR(20)) + ISNULL(' CUOTA ' + @Cuotas,'') + ' PAGO LIBRE ' + CAST(@ImporteRecibido AS VARCHAR(20)) , 
			  1, 1,@OrdenVentaId,@CreditoId, @UsuarioId , @FechaActual,@TipoPagoId)
	SET @MovimientoCajaId = @@IDENTITY

	IF @ImporteRecibido>@SumaPagoCuotaUlt 
	BEGIN
		SET @PlanPagoIdUlt = NULL
		IF LEN(@ListaPlanPagoId)>0
			SELECT TOP 1 @PlanPagoIdUlt = PlanPagoId 
			FROM CREDITO.PlanPago PP WITH(NOLOCK)
			LEFT JOIN dbo.Split(@ListaPlanPagoId,',') L ON L.Name=pp.PlanPagoId
			WHERE PP.CreditoId=@CreditoId AND PP.Estado='PEN' AND L.Name IS NULL 
			ORDER BY Numero ASC
		ELSE
			SELECT TOP 1 @PlanPagoIdUlt = PlanPagoId 
			FROM CREDITO.PlanPago PP WITH(NOLOCK)
			WHERE PP.CreditoId=@CreditoId AND PP.Estado='PEN' 
			ORDER BY Numero ASC
			
		
	END	
	
	
		
			
END	
        
-- Yape/Plin/transferencia: cuotas y pago libre deben entrar a Verificar pagos / bloqueo de cierre
IF @TipoPagoId > 1 AND @MovimientoCajaId IS NOT NULL
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM CREDITO.MovimientoCajaExtension WHERE MovimientoCajaId = @MovimientoCajaId
	)
		INSERT INTO CREDITO.MovimientoCajaExtension
			(MovimientoCajaId, FechaTransferencia, IndTransferenciaVerificada)
		VALUES (@MovimientoCajaId, @FechaPagoTransferencia, 0)
END
        
UPDATE PP
SET Estado = 'PAG',MovimientoCajaId=@MovimientoCajaId,
	FechaPagoCuota=@FechaActual, UsuarioModId=@UsuarioId,FechaMod=@FechaActual
FROM dbo.Split(@ListaPlanPagoId,',') L
INNER JOIN CREDITO.PlanPago PP WITH(NOLOCK) ON L.Name=pp.PlanPagoId
WHERE PP.Estado='PEN' AND PP.CreditoId=@CreditoId

/*actualizar caja diario*/
DECLARE @entradas DECIMAL(16,2)=0, @salidas DECIMAL(16,2)=0
SELECT @entradas=SUM(ImportePago) 
FROM CREDITO.MovimientoCaja WITH(NOLOCK)
WHERE CajaDiarioId=@CajaDiarioId AND Estado=1 AND IndEntrada=1

SELECT @salidas=SUM(ImportePago) 
FROM CREDITO.MovimientoCaja WITH(NOLOCK)
WHERE CajaDiarioId=@CajaDiarioId AND Estado=1 AND IndEntrada=0

UPDATE CREDITO.CajaDiario 
SET Entradas=ISNULL(@entradas,0) , Salidas = ISNULL(@salidas,0) , 
	SaldoFinal = SaldoInicial + ISNULL(@entradas,0) - ISNULL(@salidas,0)
WHERE CajaDiarioId=@CajaDiarioId

/*actualizar el credito*/

UPDATE	CREDITO.Credito
SET		Estado='PAG', FechaPagado=@FechaActual, UsuarioModId=@UsuarioId, FechaMod=@FechaActual
WHERE	CreditoId=@CreditoId AND
		NOT EXISTS(SELECT 1 FROM CREDITO.PlanPago WHERE CreditoId= @CreditoId AND Estado='PEN')
SELECT @MovimientoCajaId
GO


/* ---- CREDITO.usp_PagarCuotaPagoLibre ---- */


/*

EXEC CREDITO.[usp_PagarCuotaPagoLibre] 18380,55557,3.96,8

*/
CREATE OR ALTER PROC [CREDITO].[usp_PagarCuotaPagoLibre]
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
	DECLARE @OficinaCredito INT, @OficinaCaja INT

	-- Multisucursal: el cobro solo puede ir a la caja de la misma oficina del crédito.
	SELECT @OficinaCredito = OficinaId, @PersonaId = PersonaId
	FROM CREDITO.Credito WHERE CreditoId = @CreditoId
	SELECT @OficinaCaja = cj.OficinaId
	FROM CREDITO.CajaDiario cd
	INNER JOIN CREDITO.Caja cj ON cj.CajaId = cd.CajaId
	WHERE cd.CajaDiarioId = @CajaDiarioId

	IF @OficinaCredito IS NULL
	BEGIN
		RAISERROR(N'Crédito no existe.', 16, 1)
		RETURN
	END
	IF @OficinaCaja IS NULL
	BEGIN
		RAISERROR(N'Caja diario no existe.', 16, 1)
		RETURN
	END
	IF @OficinaCredito <> @OficinaCaja
	BEGIN
		RAISERROR(N'El crédito no pertenece a la oficina de la caja (multisucursal).', 16, 1)
		RETURN
	END

	DECLARE @tCuotasPendientes TABLE(Id INT IDENTITY(1,1),PlanPagoId INT,Glosa VARCHAR(MAX),FechaVencimiento DATE,Amortizacion DECIMAL(16,2),
									Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2), Cuota DECIMAL(16,2), DiasAtrazo INT,
									ImporteMora DECIMAL(16,2),Descuento DECIMAL(16,2), Cargo DECIMAL(16,2),
									PagoLibre DECIMAL(16,2),PagoCuota DECIMAL(16,2))
	INSERT INTO @tCuotasPendientes
	EXEC CREDITO.usp_CuotasPendientes @CreditoId,@FechaActual
	--SELECT * FROM @tCuotasPendientes

	
	DECLARE @MedioPago VARCHAR(80) = (
		SELECT TOP (1) vt.Denominacion
		FROM MAESTRO.ValorTabla AS vt
		WHERE vt.TablaId = 13 AND vt.ItemId = @TipoPagoId
	)

	-- PAGO IMPORTE LIBRE (glosa incluye medio: EFECTIVO / YAPE / PLIN…)
	INSERT INTO CREDITO.MovimientoCaja
			( CajaDiarioId,PersonaId ,Operacion ,ImportePago ,
			  Descripcion ,IndEntrada ,Estado,OrdenVentaId,CreditoId ,UsuarioRegId ,FechaReg,TipoPagoId)
	VALUES  ( @CajaDiarioId,@PersonaId, 'CUO' , @ImporteRecibido , 
			  'CREDITO ' + CAST(@CreditoId AS VARCHAR(20)) + ' PAGO LIBRE ' + CAST(@ImporteRecibido AS VARCHAR(20))
			  + ISNULL(' (' + @MedioPago + ')', '') , 
			  1, 1,NULL,@CreditoId, @UsuarioId , @FechaActual,@TipoPagoId)
	SET @MovimientoCajaId = @@IDENTITY
	
	
	IF @TipoPagoId>1 -- Digital/transferencia: Yape, Interbank, BCP, Banco de la Nación, …
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
GO


/* ---- CREDITO.usp_PagarCuotasCancelacion ---- */


/*
SELECT * FROM CREDITO.PlanPago WHERE CreditoId=16
EXEC [CREDITO].[usp_PagarCuotasCancelacion] 2,16,3,'20140311'
SELECT * FROM CREDITO.PlanPago WHERE CreditoId=13
SELECT * FROM CREDITO.CreditoPago

*/
CREATE OR ALTER PROC [CREDITO].[usp_PagarCuotasCancelacion]
@CajaDiarioId INT ,
@CreditoId INT,
@UsuarioId INT,
@FechaPago DATE=NULL,
@TipoPagoId INT=1,
@FechaPagoTransferencia VARCHAR(16)=''
AS
DECLARE @FechaActual DATETIME=dbo.ufnFecha()
DECLARE @ListaPlanPagoId VARCHAR(MAX)='',@PagoCuota DECIMAL(16,2)=0,@PlanPagoId INT,@Index INT=1, @SumaPagoCuota DECIMAL(16,2)=0,
		@NroCuotas INT=0
DECLARE @OficinaCredito INT, @OficinaCaja INT

-- Multisucursal: cancelación solo en caja de la misma oficina del crédito.
SELECT @OficinaCredito = OficinaId FROM CREDITO.Credito WHERE CreditoId = @CreditoId
SELECT @OficinaCaja = cj.OficinaId
FROM CREDITO.CajaDiario cd
INNER JOIN CREDITO.Caja cj ON cj.CajaId = cd.CajaId
WHERE cd.CajaDiarioId = @CajaDiarioId

IF @OficinaCredito IS NULL
BEGIN
	RAISERROR(N'Crédito no existe.', 16, 1)
	RETURN
END
IF @OficinaCaja IS NULL
BEGIN
	RAISERROR(N'Caja diario no existe.', 16, 1)
	RETURN
END
IF @OficinaCredito <> @OficinaCaja
BEGIN
	RAISERROR(N'El crédito no pertenece a la oficina de la caja (multisucursal).', 16, 1)
	RETURN
END

IF EXISTS(SELECT 1 FROM CREDITO.Credito WITH(NOLOCK) WHERE Estado<>'DES' AND CreditoId=@CreditoId)
	RETURN
	
IF @FechaPago IS NULL
	SET @FechaPago=@FechaActual
	
DECLARE @tCuotasPendientes TABLE(Id INT IDENTITY(1,1),PlanPagoId INT,Glosa VARCHAR(MAX),FechaVencimiento DATE,Amortizacion DECIMAL(16,2),
								Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2), Cuota DECIMAL(16,2), DiasAtrazo INT,
								ImporteMora DECIMAL(16,2),Descuento DECIMAL(16,2), Cargo DECIMAL(16,2),
								PagoLibre DECIMAL(16,2),PagoCuota DECIMAL(16,2))
INSERT INTO @tCuotasPendientes
EXEC CREDITO.usp_CuotasPendientes @CreditoId,@FechaPago,1

UPDATE	PP
SET		Interes = P.Interes ,Cuota=P.Cuota ,PagoCuota=P.PagoCuota 
FROM	CREDITO.PlanPago PP 
INNER JOIN @tCuotasPendientes P ON PP.PlanPagoId=P.PlanPagoId

SET @NroCuotas = (SELECT COUNT(1) FROM @tCuotasPendientes)

WHILE @Index<=@NroCuotas
BEGIN
	SELECT @PagoCuota=PagoCuota, @PlanPagoId=PlanPagoId 
	FROM @tCuotasPendientes WHERE Id=@Index
	
	SET @SumaPagoCuota=@SumaPagoCuota + @PagoCuota
	SET @ListaPlanPagoId = @ListaPlanPagoId + CAST(@PlanPagoId AS VARCHAR(10)) + ','
	SET @Index = @Index + 1
END

IF LEN(@ListaPlanPagoId)>0
BEGIN
	SET @ListaPlanPagoId = SUBSTRING(@ListaPlanPagoId,1,LEN(@ListaPlanPagoId)-1)
	EXEC CREDITO.usp_PagarCuotas
		@CajaDiarioId=@CajaDiarioId,
		@CreditoId=@CreditoId,
		@ListaPlanPagoId=@ListaPlanPagoId,
		@ImporteRecibido=@SumaPagoCuota,
		@UsuarioId=@UsuarioId,
		@FechaPago=@FechaPago,
		@TipoPagoId=@TipoPagoId,
		@FechaPagoTransferencia=@FechaPagoTransferencia
END
GO


/* ---- CREDITO.usp_CompletarImpagos ---- */


CREATE OR ALTER PROC [CREDITO].[usp_CompletarImpagos]
    @CajaDiarioId INT 
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UsuarioId INT = (
        SELECT UsuarioAsignadoId 
        FROM CREDITO.CajaDiario 
        WHERE CajaDiarioId = @CajaDiarioId
    );

    DECLARE @OficinaId INT = (
        SELECT cj.OficinaId
        FROM CREDITO.CajaDiario cd
        INNER JOIN CREDITO.Caja cj ON cj.CajaId = cd.CajaId
        WHERE cd.CajaDiarioId = @CajaDiarioId
    );

    DECLARE @Fecha DATETIME = dbo.ufnFecha() 
    
    IF EXISTS(SELECT 1 FROM CREDITO.CajaDiario WHERE CajaDiarioId = @CajaDiarioId AND IndCierre = 1)
    BEGIN
        SET @Fecha = (SELECT FechaFinOperacion FROM CREDITO.CajaDiario WHERE CajaDiarioId = @CajaDiarioId AND IndCierre = 1)
    END
		
    -- Validar que existe la caja y tiene usuario asignado
    IF @UsuarioId IS NULL OR @OficinaId IS NULL
    BEGIN
        RAISERROR('CajaDiarioId no válido o sin usuario/oficina asignados.', 16, 1);
        RETURN;
    END

    -- Inserción masiva de impagos (Monto 0.00) — solo créditos de la misma oficina de la caja.
    INSERT INTO CREDITO.MovimientoCaja (
        CajaDiarioId, PersonaId, Operacion, ImportePago,
        Descripcion, IndEntrada, Estado, OrdenVentaId, 
        CreditoId, UsuarioRegId, FechaReg, TipoPagoId
    )
    SELECT 
        @CajaDiarioId, 
        c.PersonaId, 
        'CUO', 
        0, 
        'CRED ' + CAST(c.CreditoId AS VARCHAR(20)) + ' PAGO LIBRE 0', 
        1, 1, NULL, 
        c.CreditoId, 
        @UsuarioId, 
        @Fecha,
        1 /* Impago documental: medio efectivo (importe 0; no mueve bóveda) */
    FROM CREDITO.Credito c
    WHERE c.Estado = 'DES' 
      AND c.UsuarioRegId = @UsuarioId
      AND c.OficinaId = @OficinaId
      
      -- CONDICIÓN OPTIMIZADA: Solo procesar si ya llegó o ya pasó la fecha de su primer pago
      AND c.FechaPrimerPago <= CAST(@Fecha AS DATE)
      
      -- Condición 1: No existe movimiento de caja relacionado a caja diario (no ha pagado hoy)
      AND NOT EXISTS (
          SELECT 1 
          FROM CREDITO.MovimientoCaja mc
          WHERE mc.CreditoId = c.CreditoId
            AND mc.CajaDiarioId = @CajaDiarioId
            AND mc.IndEntrada = 1 
            AND mc.Estado = 1
            AND mc.Operacion = 'CUO'
      );
END
GO


/* ---- CREDITO.usp_CompletarImpagosValidacion ---- */

-- EXEC [CREDITO].[usp_CompletarImpagosValidacion] 26080

CREATE OR ALTER PROC [CREDITO].[usp_CompletarImpagosValidacion]
@CajaDiarioId INT 
AS

DECLARE @UsuarioId INT=(select UsuarioAsignadoId from CREDITO.CajaDiario where CajaDiarioId=@CajaDiarioId)
DECLARE @OficinaId INT=(
    SELECT cj.OficinaId
    FROM CREDITO.CajaDiario cd
    INNER JOIN CREDITO.Caja cj ON cj.CajaId = cd.CajaId
    WHERE cd.CajaDiarioId = @CajaDiarioId
)
DECLARE @Impagos INT=0
DECLARE @Fecha DATETIME = dbo.ufnFecha()

SELECT	@Impagos = COUNT(1)
FROM CREDITO.Credito c
    WHERE c.Estado = 'DES' 
      AND c.UsuarioRegId = @UsuarioId
      AND c.OficinaId = @OficinaId
      -- No marcar impago antes del 1.er pago (adelanto el mismo día del desembolso es voluntario)
      AND c.FechaPrimerPago <= CAST(@Fecha AS DATE)
      -- Condición 1: No existe movimiento de caja relacionado a caja diario
      AND NOT EXISTS (
          SELECT 1 
          FROM CREDITO.MovimientoCaja mc
          WHERE mc.CreditoId = c.CreditoId
		    AND mc.CajaDiarioId = @CajaDiarioId
            AND mc.IndEntrada = 1 
            AND mc.Estado = 1
		    AND mc.Operacion = 'CUO'
      )
	  -- Condición 2: hoy hay cuota con vencimiento (día de cobro programado)
      AND EXISTS (
          SELECT 1
          FROM CREDITO.PlanPago pp
          WHERE pp.CreditoId = c.CreditoId
            AND pp.FechaVencimiento = CAST(@Fecha AS DATE)
      );

Select @Impagos 'Impagos'
GO


/* ---- CREDITO.usp_TransferirBoveda ---- */

-- Transferencia interoficina con billeteras Central (10–13):
--   Fondeo Central→sucursal: sale 10–13, llega familia genérica (2/3).
--   Remesa sucursal→Central: elige billetera destino (10–13); sale familia (2/3).
-- Evita saldos huérfanos 10–13 fuera de IndPrincipal.
-- Idempotente (CREATE OR ALTER).

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_TransferirBoveda](
    @BovedaInicioId INT,
    @BovedaDestinoId INT,
    @Glosa VARCHAR(MAX),
    @Monto DECIMAL(16, 2),
    @UsuarioRegId INT,
    @flagAceptar INT,
    @BovedaMovTempId INT = 0,
    @TipoPagoId SMALLINT = 1)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Estado BIT = 1;
    DECLARE @FechaReg DATETIME = dbo.ufnFecha();
    DECLARE @TipoPagoOrigenId SMALLINT;
    DECLARE @TipoPagoDestinoId SMALLINT;
    DECLARE @OrigenEsCentral BIT;
    DECLARE @DestinoEsCentral BIT;
    DECLARE @EsBilleteraCentral BIT;

    -- Fail-fast: no embudo silencioso a efectivo (pérdida de medio en multisucursal).
    IF ISNULL(@TipoPagoId, 0) < 1
    BEGIN
        RAISERROR(N'TipoPagoId es obligatorio; no se asume efectivo.', 16, 1);
        RETURN;
    END

    SET @TipoPagoOrigenId = @TipoPagoId;
    SET @TipoPagoDestinoId = @TipoPagoId;

    IF @flagAceptar = 0
    BEGIN
        IF @BovedaInicioId IS NULL OR @BovedaDestinoId IS NULL
            OR @BovedaInicioId < 1 OR @BovedaDestinoId < 1
        BEGIN
            RAISERROR(N'Origen y destino de bóveda son obligatorios.', 16, 1);
            RETURN;
        END

        IF @BovedaInicioId = @BovedaDestinoId
        BEGIN
            RAISERROR(N'La bóveda de origen y destino no pueden ser la misma.', 16, 1);
            RETURN;
        END

        IF @Monto IS NULL OR @Monto <= 0
        BEGIN
            RAISERROR(N'El monto debe ser mayor a cero.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM MAESTRO.ValorTabla AS vt
            WHERE vt.TablaId = 13
              AND vt.ItemId = @TipoPagoId
              AND vt.ItemId > 0
              AND vt.ItemId NOT BETWEEN 6 AND 8
              AND ISNULL(vt.DesCorta, N'') <> N'OBSOLETO'
        )
        BEGIN
            RAISERROR(N'El medio no es una marca activa.', 16, 1);
            RETURN;
        END

        SELECT @OrigenEsCentral = CAST(ISNULL(o.IndPrincipal, 0) AS bit)
        FROM CREDITO.Boveda AS b
        INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = b.OficinaId
        WHERE b.BovedaId = @BovedaInicioId;

        SELECT @DestinoEsCentral = CAST(ISNULL(o.IndPrincipal, 0) AS bit)
        FROM CREDITO.Boveda AS b
        INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = b.OficinaId
        WHERE b.BovedaId = @BovedaDestinoId;

        IF @OrigenEsCentral IS NULL OR @DestinoEsCentral IS NULL
        BEGIN
            RAISERROR(N'No se pudo resolver la oficina de origen o destino.', 16, 1);
            RETURN;
        END

        SET @EsBilleteraCentral = CASE
            WHEN EXISTS (
                SELECT 1 FROM MAESTRO.ValorTabla
                WHERE TablaId = 13 AND ItemId = @TipoPagoId AND DesCorta = N'CENTRAL')
            THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;

        IF @EsBilleteraCentral = CAST(1 AS bit)
        BEGIN
            -- Fondeo: Central tipa billetera → sucursal recibe genérico.
            IF @OrigenEsCentral = CAST(1 AS bit) AND @DestinoEsCentral = CAST(0 AS bit)
            BEGIN
                SET @TipoPagoDestinoId = CASE
                    WHEN @TipoPagoId IN (10, 11) THEN CAST(2 AS smallint)
                    WHEN @TipoPagoId IN (12, 13) THEN CAST(3 AS smallint)
                    ELSE @TipoPagoId
                END;
            END
            -- Remesa: sucursal elige billetera destino → sale genérico de familia.
            ELSE IF @OrigenEsCentral = CAST(0 AS bit) AND @DestinoEsCentral = CAST(1 AS bit)
            BEGIN
                SET @TipoPagoOrigenId = CASE
                    WHEN @TipoPagoId IN (10, 11) THEN CAST(2 AS smallint)
                    WHEN @TipoPagoId IN (12, 13) THEN CAST(3 AS smallint)
                    ELSE @TipoPagoId
                END;
            END
            ELSE
            BEGIN
                RAISERROR(
                    N'Yape/Interbank Gerente y René solo aplican en fondeo Central→sucursal o remesa sucursal→Central.',
                    16, 1);
                RETURN;
            END
        END
        ELSE IF @TipoPagoId IN (2, 3)
             AND @OrigenEsCentral = CAST(1 AS bit)
        BEGIN
            RAISERROR(
                N'En Central use Yape/Interbank Gerente o René (el genérico es el total de red).',
                16, 1);
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = @BovedaInicioId AND IndCierre = 0)
        BEGIN
            RAISERROR(N'La bóveda de origen no está abierta.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = @BovedaDestinoId AND IndCierre = 0)
        BEGIN
            RAISERROR(N'La bóveda de destino no está abierta.', 16, 1);
            RETURN;
        END

        DECLARE @HayCuentas BIT = CASE
            WHEN EXISTS (SELECT 1 FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaInicioId)
            THEN 1 ELSE 0 END;
        DECLARE @SiBoveda DECIMAL(16, 2) = (
            SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId = @BovedaInicioId);
        DECLARE @SiMedio DECIMAL(16, 2) = ISNULL((
            SELECT SaldoInicial
            FROM CREDITO.BovedaCuenta
            WHERE BovedaId = @BovedaInicioId AND TipoPagoId = @TipoPagoOrigenId), 0);
        IF @HayCuentas = 0 AND @TipoPagoOrigenId = 1
            SET @SiMedio = ISNULL(@SiBoveda, 0);
        DECLARE @NetoMedio DECIMAL(16, 2) = ISNULL((
            SELECT SUM(IIF(IndEntrada = 1, Importe, -Importe))
            FROM CREDITO.BovedaMov
            WHERE BovedaId = @BovedaInicioId AND Estado = 1 AND TipoPagoId = @TipoPagoOrigenId), 0);

        IF (@SiMedio + @NetoMedio) < @Monto
        BEGIN
            RAISERROR(N'Saldo insuficiente en el medio de origen.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (
            SELECT 1 FROM CREDITO.BovedaCuenta
            WHERE BovedaId = @BovedaDestinoId AND TipoPagoId = @TipoPagoDestinoId
        )
        BEGIN
            INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
            VALUES (@BovedaDestinoId, @TipoPagoDestinoId, CAST(0 AS decimal(16, 2)));
        END

        INSERT INTO CREDITO.BovedaMov (
            BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (
            @BovedaInicioId, 'TRS', @Glosa, @Monto, 0, @Estado, @UsuarioRegId, @FechaReg, @TipoPagoOrigenId);
        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId;

        INSERT INTO CREDITO.BovedaMov (
            BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado, UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (
            @BovedaDestinoId, 'TRE', @Glosa, @Monto, 1, @Estado, @UsuarioRegId, @FechaReg, @TipoPagoDestinoId);
        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaDestinoId;
    END

    IF @flagAceptar = 2
    BEGIN
        DELETE CREDITO.BovedaMov
        WHERE MovimientoBovedaId = (
            SELECT MovimientoBovedaIniId FROM CREDITO.BovedaMovTemp WHERE BovedaMovTempId = @BovedaMovTempId);

        SET @BovedaInicioId = (
            SELECT BovedaInicioId FROM CREDITO.BovedaMovTemp WHERE BovedaMovTempId = @BovedaMovTempId);

        EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId;

        DELETE CREDITO.BovedaMovTemp WHERE BovedaMovTempId = @BovedaMovTempId;
    END
END
GO


/* ---- CREDITO.usp_RptCreditoRentabilidad ---- */


-- CREDITO.usp_RptCreditoRentabilidad 1,'20200301','20200330'
CREATE OR ALTER PROC [CREDITO].[usp_RptCreditoRentabilidad]
@OficnaId INT = NULL,
@FechaIni DATE ,
@FechaFin DATE,
@EstadoCredito char(3) 
AS

;WITH CREDITOSUM AS(
	SELECT CR.CreditoId,COUNT(1) 'CuotasPagadas', SUM(PP.Interes) 'SumInteres',
	SUM(PP.Cuota) 'SumCuota',SUM(PP.ImporteMora) 'SumMora',
	/* Solo cuotas PAG: PagoCuota es el cobrado (no sumar PagoLibre otra vez). */
	SUM(ISNULL(PP.PagoCuota, 0)) 'SumPago',MAX(PP.FechaPagoCuota) 'FechaPago'
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
GO


/* ---- CREDITO.usp_RptCreditosActivos ---- */

--exec CREDITO.usp_RptCreditosActivos '20250901','20250930',8

CREATE OR ALTER PROC [CREDITO].[usp_RptCreditosActivos]
@FechaIni  DATE,
@FechaFin  DATE,
@UsuarioId INT = NULL,
@OficinaId INT = NULL
AS

DECLARE @FechaAct DATE = dbo.ufnFecha();

;WITH AGG_PP AS (
    SELECT
        CreditoId,
        MIN(CASE WHEN Estado = 'PEN' THEN FechaVencimiento END)                AS MinFechaVenPen,
        COUNT(CASE WHEN Estado = 'PEN' THEN 1 END)                             AS NroCuotasPen,
        SUM(CASE WHEN Estado = 'PEN' THEN Cuota - PagoLibre  ELSE 0 END)      AS Saldo,
        SUM(CASE WHEN Estado = 'PEN' THEN Amortizacion       ELSE 0 END)      AS SaldoCapital,
        ABS(SUM(CASE WHEN Estado = 'PEN' THEN Interes - PagoLibre ELSE 0 END))AS SaldoInteres,
        /* Pagado real: PAG→PagoCuota; PEN→PagoLibre (no sumar ambos). */
        SUM(CASE
                WHEN Estado = 'PAG' THEN ISNULL(PagoCuota, 0)
                ELSE ISNULL(PagoLibre, 0)
            END)                                                              AS Pagado,
        SUM(CASE WHEN Estado = 'PAG' THEN ISNULL(Interes, 0) ELSE 0 END)      AS InteresPagado,
        SUM(Interes)                                                            AS MontoInteres,
        SUM(Cuota)                                                              AS MontoCreditoTotal,
        SUM(ImporteMora)                                                        AS TotalImporteMora,
        MAX(CASE WHEN Numero = 1 THEN Cuota END)                               AS CuotaNro1
    FROM CREDITO.PlanPago
    GROUP BY CreditoId
),
CARTERA AS (
    -- Créditos DESEMBOLSADOS activos (cartera viva)
    SELECT
        C.CreditoId,
        dbo.ufnCalcularDiasAtrazo(AP.MinFechaVenPen, @FechaAct)               AS DiasAtrazo,
        AP.NroCuotasPen,
        AP.Saldo,
        AP.SaldoCapital,
        AP.SaldoInteres,
        AP.Pagado,
        AP.InteresPagado,
        AP.MontoInteres,
        AP.MontoCreditoTotal,
        CONVERT(DECIMAL(10,2), AP.TotalImporteMora)                            AS Mora,
        AP.CuotaNro1                                                            AS Cuota
    FROM CREDITO.Credito C
    INNER JOIN AGG_PP AP ON C.CreditoId = AP.CreditoId
    WHERE C.Estado = 'DES'
      AND C.OficinaId    = ISNULL(@OficinaId, C.OficinaId)
      AND C.UsuarioRegId = ISNULL(@UsuarioId, C.UsuarioRegId)

    UNION ALL

    -- Créditos PAGADOS cuyo 1.er pago cae en el rango (histórico del periodo)
    SELECT
        C.CreditoId,
        0                                                                       AS DiasAtrazo,
        0                                                                       AS NroCuotasPen,
        0.0                                                                       AS Saldo,
        0.0                                                                       AS SaldoCapital,
        0.0                                                                       AS SaldoInteres,
        AP.Pagado,
        AP.InteresPagado,
        AP.MontoInteres,
        AP.MontoCreditoTotal,
        CONVERT(DECIMAL(10,2), AP.TotalImporteMora)                            AS Mora,
        AP.CuotaNro1                                                            AS Cuota
    FROM CREDITO.Credito C
    INNER JOIN AGG_PP AP ON C.CreditoId = AP.CreditoId
    WHERE C.Estado = 'PAG'
      AND C.FechaPrimerPago BETWEEN @FechaIni AND @FechaFin
      AND C.OficinaId    = ISNULL(@OficinaId, C.OficinaId)
      AND C.UsuarioRegId = ISNULL(@UsuarioId, C.UsuarioRegId)
)
SELECT
    ROW_NUMBER() OVER (ORDER BY C.Estado, A.NombreCompleto,P.NombreCompleto)          AS Nro,
    CASE C.Estado WHEN 'DES' THEN 'DESEMBOLSADO' WHEN 'PAG' THEN 'PAGADO' END AS Estado,
    A.NombreCompleto                                                            AS Agente,
    C.CreditoId,
    P.NombreCompleto                                                            AS Cliente,
    C.MontoCredito,
    C.FormaPago,
    C.NumeroCuotas,
    C.Interes,
    CM.MontoInteres,
    CM.MontoCreditoTotal,
    C.MontoGastosAdm,
    C.CentralRiesgo,
    C.FechaPrimerPago,
    C.FechaVencimiento,
    CM.Cuota,
    (C.NumeroCuotas - CM.NroCuotasPen)                                         AS NroCuotasPagado,
    CM.Pagado,
    ISNULL(CM.InteresPagado, 0)                                                AS InteresPagado,
    CM.NroCuotasPen,
    CM.SaldoCapital,
    CM.SaldoInteres,
    CM.Saldo,
    CM.DiasAtrazo,
    CONVERT(DECIMAL(10,2), CM.Mora)                                            AS Mora
FROM CREDITO.Credito C
INNER JOIN CARTERA CM        ON C.CreditoId    = CM.CreditoId
INNER JOIN MAESTRO.Persona P ON C.PersonaId    = P.PersonaId
INNER JOIN MAESTRO.Usuario u ON C.UsuarioRegId = u.UsuarioId
INNER JOIN MAESTRO.Persona A ON u.PersonaId    = A.PersonaId
ORDER BY C.Estado, A.NombreCompleto, P.NombreCompleto;
GO


/* ---- CREDITO.usp_RptSaldosCajaResumenTipoCuenta ---- */


--Exec CREDITO.usp_RptSaldosCajaResumenTipoCuenta @OficinaId=1
CREATE OR ALTER PROC [CREDITO].[usp_RptSaldosCajaResumenTipoCuenta]
@OficinaId INT
AS

IF @OficinaId IS NULL OR @OficinaId < 1
BEGIN
    RAISERROR(N'@OficinaId es obligatorio (multisucursal).', 16, 1);
    RETURN;
END

-- Saldo inicial efectivo solo de la oficina (no de toda la red).
DECLARE @SaldoInicial DECIMAL(15,2) = ISNULL((
    SELECT SUM(CD.SaldoInicial)
    FROM CREDITO.CajaDiario CD
    INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
    WHERE CD.TransBoveda = 0
      AND C.OficinaId = @OficinaId
), 0);


;WITH SALDOINICIAL AS(
	SELECT ItemId 'TipoPagoId',Denominacion, IIF(ItemId=1,@SaldoInicial,0) 'Importe'
	FROM MAESTRO.ValorTabla
	WHERE TablaId=13 AND ItemId>0
),
CUENTAS AS (
	SELECT TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.ImportePago,-MC.ImportePago) ) 'Importe'
	FROM CREDITO.CajaDiario CD
	INNER JOIN	CREDITO.MovimientoCaja MC  ON MC.CajaDiarioId = CD.CajaDiarioId
	INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
	WHERE  CD.TransBoveda=0 AND C.OficinaId=@OficinaId
	AND MC.Estado=1
	GROUP BY MC.TipoPagoId
), SALDOS AS(
	SELECT S.TipoPagoId, S.Denominacion, S.Importe + ISNULL(c.Importe,0) 'Saldo'
	FROM SALDOINICIAL S
	LEFT JOIN CUENTAS C ON  c.TipoPagoId= S.TipoPagoId
)
SELECT 'RESUMEN CUENTA: ' + STRING_AGG(Denominacion + ' = ' + CAST (Saldo AS VARCHAR(15)),'  ') 'Total'
FROM SALDOS
WHERE Saldo>0
GO


PRINT N'OK 2026-11-07 AZURE multisucursal batch aplicado.';
GO
