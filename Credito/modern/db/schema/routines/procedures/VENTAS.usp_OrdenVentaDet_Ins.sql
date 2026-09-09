

/*
SELECT SA.* FROM VENTAS.ListaPrecio LP
INNER JOIN ALMACEN.SerieArticulo SA ON LP.ArticuloId = SA.ArticuloId AND EstadoId=2

SELECT * FROM VENTAS.OrdenVenta 
SELECT * FROM VENTAS.OrdenVentaDet 
SELECT * FROM VENTAS.OrdenVentaDetSerie 

EXEC VENTAS.usp_OrdenVentaDet_Ins @OficinaId = 1, @OrdenVentaId = 17, @NumeroSerie = '10006'

DELETE FROM VENTAS.OrdenVentaDetSerie
DELETE FROM VENTAS.OrdenVentaDet
DELETE FROM VENTAS.OrdenVenta
UPDATE ALMACEN.SerieArticulo SET EstadoId=2

*/

CREATE PROC [VENTAS].[usp_OrdenVentaDet_Ins]
@OrdenVentaId INT, 
@NumeroSerie VARCHAR(20),
@UsuarioId INT=1
AS
BEGIN
	
	DECLARE @ArticuloId INT, @Mensaje VARCHAR(255), @EstadoEnAlmacen INT
	DECLARE @Decripcion VARCHAR(250), @PrecioUnitario DECIMAL(16,2),@OrdenVentaDetId INT,@SerieArticuloId INT, @IGV DECIMAL(16,2)
	SET @EstadoEnAlmacen = 2
	SET @IGV = 0.18

	IF NOT EXISTS(SELECT 1 FROM ALMACEN.SerieArticulo WHERE NumeroSerie=@NumeroSerie)
		SET @Mensaje='No existe Artículo !!!'

	ELSE IF NOT EXISTS(SELECT 1 FROM ALMACEN.SerieArticulo WHERE NumeroSerie=@NumeroSerie AND EstadoId=@EstadoEnAlmacen)
		SELECT	@Mensaje='El articulo ' + A.Denominacion + ' se encuentra en estado ' + E.Denominacion
		FROM	ALMACEN.SerieArticulo SA
		INNER	JOIN ALMACEN.Articulo A ON SA.ArticuloId = A.ArticuloId
		INNER	JOIN MAESTRO.ValorTabla E ON E.TablaId=6 AND E.ItemId = SA.EstadoId
		WHERE	NumeroSerie=@NumeroSerie

	ELSE IF NOT EXISTS( SELECT	1 FROM	ALMACEN.SerieArticulo SA
						INNER	JOIN ALMACEN.Articulo A ON SA.ArticuloId = A.ArticuloId AND A.Estado=1
						INNER	JOIN VENTAS.ListaPrecio LP ON A.ArticuloId = LP.ArticuloId AND LP.Estado=1
						WHERE	NumeroSerie=@NumeroSerie AND SA.EstadoId=@EstadoEnAlmacen)
		SELECT	@Mensaje='No existe Lista de Precio para el artículo ' + A.Denominacion
		FROM	ALMACEN.SerieArticulo SA
		INNER	JOIN ALMACEN.Articulo A ON SA.ArticuloId = A.ArticuloId
		WHERE	NumeroSerie = @NumeroSerie


	SET @Mensaje = ISNULL(@Mensaje,'')
	IF LEN(@Mensaje)>0
	BEGIN
		SELECT @Mensaje 'Mensaje'		
		RETURN
	END	
		
	--IF	@OrdenVentaId = 0
	--BEGIN
	--	INSERT INTO VENTAS.OrdenVenta
	--	(PersonaId, OficinaId , Observacion ,Subtotal ,TotalDescuento ,TotalImpuesto ,TotalNeto ,IndEntregado ,Estado,UsuarioRegId,FechaReg)
	--	VALUES (@PersonaId, @OficinaId , '' , 0.0 , 0.0 , 0.0 , 0.0 , 0 , 1,@UsuarioId,dbo.ufnFecha())
	--	SELECT @OrdenVentaId=@@IDENTITY
	--END

	SELECT	@ArticuloId=SA.ArticuloId,
			@PrecioUnitario=LP.Monto,
			@SerieArticuloId = SA.SerieArticuloId,
			@Decripcion = Denominacion + ' SN: ' + @NumeroSerie
	FROM ALMACEN.SerieArticulo SA	
	INNER JOIN ALMACEN.Articulo A ON SA.ArticuloId = A.ArticuloId
	INNER JOIN VENTAS.ListaPrecio LP ON A.ArticuloId = LP.ArticuloId AND LP.Estado=1
	WHERE SA.NumeroSerie = @NumeroSerie

	IF NOT EXISTS(SELECT 1 FROM VENTAS.OrdenVentaDet WHERE Estado=1 AND OrdenVentaId=@OrdenVentaId AND ArticuloId=@ArticuloId)
		BEGIN		
					
			INSERT INTO VENTAS.OrdenVentaDet
			(OrdenVentaId ,ArticuloId ,Cantidad ,Descripcion ,ValorVenta ,Descuento ,Subtotal ,Estado)
			VALUES  
			(@OrdenVentaId,@ArticuloId,1,@Decripcion,@PrecioUnitario,0.0,@PrecioUnitario, 1)
			
			SELECT @OrdenVentaDetId=@@IDENTITY
			
		END
	ELSE
			SELECT @OrdenVentaDetId = OrdenVentaDetId
			FROM VENTAS.OrdenVentaDet WHERE Estado=1 AND OrdenVentaId=@OrdenVentaId AND ArticuloId=@ArticuloId

	INSERT INTO VENTAS.OrdenVentaDetSerie ( OrdenVentaDetId,SerieArticuloId)
	VALUES  ( @OrdenVentaDetId , @SerieArticuloId )


	DECLARE @lstSerie VARCHAR(MAX)
	SELECT	@lstSerie =  ISNULL(@lstSerie + ',','') + SA.NumeroSerie
	FROM	VENTAS.OrdenVentaDetSerie S
	INNER JOIN ALMACEN.SerieArticulo SA ON S.SerieArticuloId = SA.SerieArticuloId
	WHERE	S.OrdenVentaDetId = @OrdenVentaDetId

	UPDATE D
	SET D.Descripcion = A.Denominacion + ' SN: ' + @lstSerie,
		Cantidad = (SELECT COUNT(1) FROM VENTAS.OrdenVentaDetSerie WHERE OrdenVentaDetId=@OrdenVentaDetId)
	FROM	VENTAS.OrdenVentaDet D
	INNER JOIN ALMACEN.Articulo A ON D.ArticuloId = A.ArticuloId
	WHERE	OrdenVentaDetId = @OrdenVentaDetId

	UPDATE VENTAS.OrdenVentaDet
	SET Subtotal = Cantidad * (ValorVenta - Descuento)
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
	OV.TotalImpuesto = OD.Subtotal * ( @IGV/(1+@IGV) ) ,
	OV.UsuarioModId = @UsuarioId,
	OV.FechaMod = dbo.ufnFecha()
	FROM VENTAS.OrdenVenta OV
	INNER JOIN OrdenDetalle OD ON OV.OrdenVentaId = OD.OrdenVentaId
	WHERE OV.OrdenVentaId = @OrdenVentaId

	UPDATE ALMACEN.SerieArticulo SET EstadoId=3 
	WHERE SerieArticuloId = @SerieArticuloId

	SELECT CAST(@OrdenVentaId AS VARCHAR(12)) 'Mensaje'
END
