--BEGIN TRANSACTION
--	DECLARE @OrdenVentaId INT = 68
--	SELECT * FROM VENTAS.OrdenVenta WHERE OrdenVentaId=@OrdenVentaId
--	SELECT * FROM ALMACEN.SerieArticulo WHERE SerieArticuloId=10180
--	SELECT * FROM CREDITO.CuentaxCobrar WHERE CuentaxCobrarId=9

--	EXEC VENTAS.usp_OrdenVenta_Del @OrdenVentaId = @OrdenVentaId

--	SELECT * FROM VENTAS.OrdenVenta WHERE OrdenVentaId=@OrdenVentaId
--	SELECT * FROM ALMACEN.SerieArticulo WHERE SerieArticuloId=10180
--	SELECT * FROM CREDITO.CuentaxCobrar WHERE CuentaxCobrarId=9

--ROLLBACK TRANSACTION

CREATE PROC [VENTAS].[usp_OrdenVenta_Del]
@OrdenVentaId INT=0,
@OrdenVentaDetId INT=0 
AS
BEGIN

	DECLARE @IGV DECIMAL(16,2),@EstadoEnAlmacen INT
	SET @IGV = 0.18
	SET @EstadoEnAlmacen=2
	
	--Eliminacion de Orden de Venta
	IF @OrdenVentaId>0
	BEGIN
		DECLARE @IdRef INT 
		
		IF EXISTS(SELECT 1 FROM VENTAS.OrdenVenta WHERE OrdenVentaId=@OrdenVentaId AND Estado='ENT' )
			RETURN		
			
		--IF EXISTS(SELECT 1 FROM VENTAS.OrdenVenta WHERE OrdenVentaId=@OrdenVentaId AND IndContado=1)
		--BEGIN
		--	SELECT @IdRef=CuentaxCobrarId FROM VENTAS.OrdenVenta WHERE OrdenVentaId=@OrdenVentaId
		--	UPDATE VENTAS.OrdenVenta SET CuentaxCobrarId=NULL WHERE OrdenVentaId=@OrdenVentaId
		--	DELETE FROM CREDITO.CuentaxCobrar WHERE CuentaxCobrarId=@IdRef
		--END
		IF EXISTS(SELECT 1 FROM VENTAS.OrdenVenta WHERE OrdenVentaId=@OrdenVentaId AND TipoVenta='CRE')
		BEGIN
			SELECT @IdRef=CreditoId FROM CREDITO.Credito WHERE OrdenVentaId=@OrdenVentaId 
			--UPDATE VENTAS.OrdenVenta SET CreditoId=NULL WHERE OrdenVentaId=@OrdenVentaId
			DELETE FROM CREDITO.PlanPago WHERE CreditoId=@IdRef
			DELETE FROM CREDITO.Credito WHERE CreditoId=@IdRef
		END
		
		UPDATE SA
		SET SA.EstadoId = @EstadoEnAlmacen
		FROM VENTAS.OrdenVentaDet OVD
		INNER JOIN VENTAS.OrdenVentaDetSerie OVDS ON OVD.OrdenVentaDetId = OVDS.OrdenVentaDetId
		INNER JOIN ALMACEN.SerieArticulo SA ON SA.SerieArticuloId=OVDS.SerieArticuloId
		WHERE OVD.OrdenVentaId=@OrdenVentaId
			
		DELETE OVDS
		FROM VENTAS.OrdenVentaDet OVD
		INNER JOIN VENTAS.OrdenVentaDetSerie OVDS ON OVD.OrdenVentaDetId = OVDS.OrdenVentaDetId
		WHERE OVD.OrdenVentaId=@OrdenVentaId
		
		DELETE FROM VENTAS.OrdenVentaDet
		WHERE OrdenVentaId = @OrdenVentaId
		
		DELETE FROM VENTAS.OrdenVenta
		WHERE OrdenVentaId = @OrdenVentaId

		
		
		RETURN
	END

	--Eliminacion de Detalle Orden de Venta
	IF @OrdenVentaDetId>0
	BEGIN
			
		SELECT @OrdenVentaId = OrdenVentaId 
		FROM VENTAS.OrdenVentaDet
		WHERE OrdenVentaDetId = @OrdenVentaDetId
		
		UPDATE SA
		SET SA.EstadoId = @EstadoEnAlmacen
		FROM VENTAS.OrdenVentaDetSerie OVDS
		INNER JOIN ALMACEN.SerieArticulo SA ON SA.SerieArticuloId=OVDS.SerieArticuloId
		WHERE OVDS.OrdenVentaDetId=@OrdenVentaDetId
				
		DELETE FROM VENTAS.OrdenVentaDetSerie 
		WHERE OrdenVentaDetId = @OrdenVentaDetId
		
		DELETE FROM VENTAS.OrdenVentaDet
		WHERE OrdenVentaDetId = @OrdenVentaDetId

		;WITH OrdenDetalle AS(
			SELECT OrdenVentaId, SUM(Subtotal) 'Subtotal' , SUM(Descuento) 'Descuento'
			FROM VENTAS.OrdenVentaDet WHERE OrdenVentaId=@OrdenVentaId AND Estado=1
			GROUP BY OrdenVentaId
		)
		UPDATE OV
		SET OV.TotalNeto = OD.Subtotal,
		OV.TotalDescuento = OD.Descuento,
		OV.Subtotal = OD.Subtotal / (1 + @IGV),
		OV.TotalImpuesto = OD.Subtotal * ( @IGV/(1+@IGV) ) 
		FROM VENTAS.OrdenVenta OV
		INNER JOIN OrdenDetalle OD ON OV.OrdenVentaId = OD.OrdenVentaId
		WHERE OV.OrdenVentaId = @OrdenVentaId
		
		IF NOT EXISTS(SELECT 1 FROM VENTAS.OrdenVentaDet WHERE OrdenVentaId = @OrdenVentaId )
			UPDATE VENTAS.OrdenVenta 
			SET Subtotal = 0, TotalDescuento = 0,TotalImpuesto = 0, TotalNeto = 0
			WHERE OrdenVentaId = @OrdenVentaId
				
	END
END
