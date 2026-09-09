

/* 

SELECT * FROM CREDITO.Credito	
SELECT * FROM CREDITO.PlanPago
SELECT * FROM CREDITO.SolicitudCredito
SELECT * FROM CREDITO.CuentaxCobrar
SELECT * FROM VENTAS.OrdenVenta


[CREDITO].[usp_Credito_Upd] 1, 29,1
*/



CREATE PROC [CREDITO].[usp_Credito_Upd]
@Opcion INT = 1,
@CreditoId INT = 0,
@UsuarioId INT 
AS
BEGIN
	DECLARE @FechaActual DATETIME = dbo.ufnFecha()
	IF @Opcion = 0 -- PRIMERA APROBACION
	BEGIN
		UPDATE CREDITO.Credito SET Estado='AP1' WHERE CreditoId=@CreditoId
		INSERT INTO CREDITO.Aprobacion
		        ( CreditoId, Nivel, UsuarioId, Fecha )
		VALUES  ( @CreditoId, 1, @UsuarioId, @FechaActual  )

	END

	IF @Opcion = 1 -- APROBAR CREDITO - SEGUNDA APROBACION
	BEGIN
		DECLARE @TipoMovSalidaxVenta INT=2 , @EstMovVendido INT =3, @MovimientoId INT,@AlmacenId INT,
		@OrdenVentaId INT , @EstadoVendido INT=4
		
		IF EXISTS(SELECT 1 FROM CREDITO.Credito WHERE CreditoId=@CreditoId AND MontoInicial>0)
		BEGIN
			INSERT INTO CREDITO.CuentaxCobrar( Operacion,Monto ,Estado ,CreditoId)
			SELECT 'INI',MontoInicial,'PEN',CreditoId
			FROM CREDITO.Credito WHERE CreditoId=@CreditoId
		END
		IF EXISTS(SELECT 1 FROM CREDITO.Credito WHERE CreditoId=@CreditoId AND MontoGastosAdm>0)
		BEGIN
			IF NOT EXISTS(SELECT 1 FROM CREDITO.PlanPago WHERE CreditoId=@CreditoId AND GastosAdm>0)
				INSERT INTO CREDITO.CuentaxCobrar( Operacion,Monto ,Estado ,CreditoId)
				SELECT 'GAD',MontoGastosAdm + CentralRiesgo ,'PEN',CreditoId
				FROM CREDITO.Credito WHERE CreditoId=@CreditoId
		END
		
		UPDATE CREDITO.Credito 
		SET Estado='APR',FechaAprobacion=@FechaActual,
			FechaMod=@FechaActual, UsuarioModId=@UsuarioId
		WHERE CreditoId = @CreditoId

		INSERT INTO CREDITO.Aprobacion
		        ( CreditoId, Nivel, UsuarioId, Fecha )
		VALUES  ( @CreditoId, 2, @UsuarioId, @FechaActual  )
		
		UPDATE CREDITO.PlanPago 
		SET Estado='PEN', FechaMod=@FechaActual, UsuarioModId=@UsuarioId
		WHERE CreditoId = @CreditoId
				
		
		SELECT @OrdenVentaId = OrdenVentaId 
		FROM CREDITO.Credito
		WHERE CreditoId=@CreditoId
		
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
	
	--SELECT * FROM ALMACEN.SerieArticulo
END
