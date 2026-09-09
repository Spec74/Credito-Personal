/*
EXEC [CREDITO].[usp_PagarCuentaxCobrar] 0,3,2,3
*/
CREATE PROC [CREDITO].[usp_PagarCuentaxCobrar]
@OrdenVentaId INT=0,
@CuentaxCobrarId INT=0,
@CajaDiarioId INT,
@UsuarioId INT 
AS
DECLARE @FechaActual DATETIME=dbo.ufnFecha()
DECLARE @MovimientoCajaId INT, @EstadoVendido INT=4,@CreditoId INT,@Operacion CHAR(3)

IF @CuentaxCobrarId > 0--CREDITO
BEGIN	
	DECLARE @glosa VARCHAR(max)
	
	SELECT	@OrdenVentaId=C.OrdenVentaId, @CreditoId=C.CreditoId, @Operacion=CXC.Operacion,
			@glosa='CREDITO ' + CONVERT(VARCHAR(15),C.CreditoId) + ' ' + C.Observacion
	FROM CREDITO.CuentaxCobrar CXC 
	INNER JOIN CREDITO.Credito C ON CXC.CreditoId = C.CreditoId
	WHERE CXC.CuentaxCobrarId=@CuentaxCobrarId
	   	
	INSERT INTO CREDITO.MovimientoCaja
			( CajaDiarioId ,Operacion,ImportePago ,
			  PersonaId ,Descripcion ,IndEntrada ,Estado,OrdenVentaId,CreditoId ,UsuarioRegId ,FechaReg )
	SELECT @CajaDiarioId,C.Operacion ,C.Monto,
			CR.PersonaId,@glosa, OP.IndEntrada,1 'Estado',@OrdenVentaId,@CreditoId,@UsuarioId , @FechaActual
	FROM CREDITO.CuentaxCobrar C 
	INNER JOIN CREDITO.Credito CR ON C.CreditoId = CR.CreditoId
	INNER JOIN MAESTRO.TipoOperacion OP ON OP.Codigo=C.Operacion
	WHERE C.CuentaxCobrarId=@CuentaxCobrarId

	SELECT @MovimientoCajaId=@@IDENTITY
	
	UPDATE CREDITO.CuentaxCobrar
	SET Estado= 'CAN', MovimientoCajaId=@MovimientoCajaId
	WHERE CuentaxCobrarId = @CuentaxCobrarId

	IF @Operacion='CDN'
	BEGIN
		UPDATE CREDITO.Credito
		SET Estado= 'PAG', 
		UsuarioModId=@UsuarioId , FechaMod =dbo.ufnFecha() 
		WHERE CreditoId = @CreditoId
	END

END
ELSE --CONTADO
BEGIN
	INSERT INTO CREDITO.MovimientoCaja
			( CajaDiarioId ,Operacion,ImportePago ,
			  PersonaId ,Descripcion ,IndEntrada ,Estado,OrdenVentaId,CreditoId ,UsuarioRegId ,FechaReg )
	SELECT @CajaDiarioId,'CON' Operacion ,OV.TotalNeto,
			OV.PersonaId,'ORDEN:' + CONVERT(VARCHAR(15),@OrdenVentaId), 1 'IndEntrada',1 'Estado',@OrdenVentaId,NULL 'CreditoId',@UsuarioId , @FechaActual
	FROM VENTAS.OrdenVenta OV 
	WHERE OV.OrdenVentaId=@OrdenVentaId
	
	SELECT @MovimientoCajaId=@@IDENTITY	
	
	DECLARE @AlmacenId INT,@MovimientoId INT,@TipoMovSalidaxVenta INT=2 , @EstMovVendido INT =3	
	IF EXISTS(SELECT 1 FROM VENTAS.OrdenVenta WHERE OrdenVentaId = @OrdenVentaId AND Estado='ENT')
		RETURN
				
	SELECT @AlmacenId= AlmacenId
	FROM	VENTAS.OrdenVenta OV
	INNER JOIN ALMACEN.Almacen A ON OV.OficinaId = A.OficinaId AND A.Estado=1
	WHERE	OV.OrdenVentaId = @OrdenVentaId
		
	INSERT INTO ALMACEN.Movimiento ( TipoMovimientoId ,AlmacenId ,Fecha ,SubTotal ,IGV , 
			AjusteRedondeo ,TotalImporte ,EstadoId ,Observacion )
	SELECT	@TipoMovSalidaxVenta 'TipoMovimientoId', @AlmacenId 'AlmacenId', @FechaActual 'Fecha', OV.Subtotal,OV.TotalImpuesto,0 'AjusteRedondeo',
			OV.TotalNeto, @EstMovVendido 'EstadoId', 'Nro Orden:' + CAST(@OrdenVentaId AS VARCHAR(20)) 'Observacion'	
	FROM	VENTAS.OrdenVenta OV
	WHERE	OV.OrdenVentaId = @OrdenVentaId
	
	SELECT @MovimientoId=@@IDENTITY
		
	INSERT INTO ALMACEN.MovimientoDet
	(	MovimientoId ,ArticuloId ,Cantidad ,Descripcion ,PrecioUnitario ,
		Descuento ,Importe ,IndCorrelativo )
	SELECT	@MovimientoId 'MovimientoId', ArticuloId,Cantidad,Descripcion,
			ValorVenta,Descuento,Subtotal,0 'IndicadorCorrelativo'
	FROM	VENTAS.OrdenVentaDet
	WHERE	OrdenVentaId = @OrdenVentaId
	
	UPDATE	VENTAS.OrdenVenta 
	SET		Estado='ENT',MovimientoAlmacenId=@MovimientoId,FechaMod=@FechaActual,UsuarioModId=@UsuarioId
	WHERE	OrdenVentaId = @OrdenVentaId
			
	UPDATE	SA
	SET		SA.EstadoId=@EstadoVendido, 
			SA.MovimientoDetSalId = MD.MovimientoDetId
	FROM VENTAS.OrdenVenta OV
	INNER JOIN VENTAS.OrdenVentaDet OVD ON OV.OrdenVentaId = OVD.OrdenVentaId
	INNER JOIN VENTAS.OrdenVentaDetSerie OVDS ON OVD.OrdenVentaDetId = OVDS.OrdenVentaDetId
	INNER JOIN ALMACEN.SerieArticulo SA ON OVDS.SerieArticuloId = SA.SerieArticuloId
	INNER JOIN ALMACEN.Movimiento M ON OV.MovimientoAlmacenId=M.MovimientoId
	INNER JOIN ALMACEN.MovimientoDet MD ON M.MovimientoId = MD.MovimientoId AND MD.ArticuloId=SA.ArticuloId
	WHERE OV.OrdenVentaId = @OrdenVentaId
END
	
/*actualizar caja diario*/
DECLARE @entradas DECIMAL(16,2)=0, @salidas DECIMAL(16,2)=0
SELECT @entradas=SUM(ImportePago) 
FROM CREDITO.MovimientoCaja
WHERE CajaDiarioId=@CajaDiarioId AND Estado=1 AND IndEntrada=1

SELECT @salidas=SUM(ImportePago) 
FROM CREDITO.MovimientoCaja
WHERE CajaDiarioId=@CajaDiarioId AND Estado=1 AND IndEntrada=0

UPDATE CREDITO.CajaDiario 
SET Entradas=ISNULL(@entradas,0) , Salidas = ISNULL(@salidas,0) , 
	SaldoFinal = SaldoInicial + ISNULL(@entradas,0) - ISNULL(@salidas,0)
WHERE CajaDiarioId=@CajaDiarioId

SELECT @MovimientoCajaId
