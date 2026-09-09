/* 

	EXEC ALMACEN.usp_CrearMovimientoDet @MovimientoId=18,@ArticuloId=1,@ListaSerie='99669966996888889658',@IndCorrelativo=1,
	@PrecioUnitario=55.08,@Descuento=45.76,@Cantidad=50

	SELECT * FROM ALMACEN.Movimiento 
	SELECT * FROM ALMACEN.MovimientoDet 
	SELECT * FROM ALMACEN.SerieArticulo 

delete from ALMACEN.SerieArticulo
delete from ALMACEN.MovimientoDet
*/


CREATE PROC [ALMACEN].[usp_CrearMovimientoDet]
@MovimientoId INT,
@MovimientoDetId INT=0,
@ArticuloId INT,
@IndAutogenerar BIT=0,
@ListaSerie VARCHAR(MAX),
@Cantidad INT=0,
@IndCorrelativo BIT=0,
@PrecioUnitario DECIMAL(16,2)=0,	
@Descuento DECIMAL(16,2)=0,	
@Medida INT=0
AS
BEGIN
	
	DECLARE @EstadoSerie INT,@Importe DECIMAL(16,2),@IGV DECIMAL(16,2), @Descripcion VARCHAR(MAX),@AlmacenId INT
	DECLARE @CantidadLista INT
	SET @EstadoSerie = 1 --sin confirmar
	SET @ListaSerie = RTRIM(LTRIM(@ListaSerie))
	SET @IGV = 0.18
		
	SELECT @Descripcion = (Denominacion + CHAR(13)) FROM ALMACEN.Articulo WHERE ArticuloId=@ArticuloId
	SELECT @AlmacenId=AlmacenId FROM ALMACEN.Movimiento WHERE MovimientoId=@MovimientoId
	IF @IndAutogenerar=0 AND @IndCorrelativo=0
		SELECT @Cantidad = COUNT(1) FROM dbo.Split(@ListaSerie,',')
	
	SET @Importe = @Cantidad * (@PrecioUnitario - @Descuento)
	
	IF @MovimientoDetId=0
	BEGIN
		INSERT INTO ALMACEN.MovimientoDet
				( MovimientoId ,ArticuloId ,Cantidad ,Descripcion , PrecioUnitario ,
				Descuento ,Importe ,UnidadMedidaT10 , IndCorrelativo )
		VALUES  ( @MovimientoId , @ArticuloId, @Cantidad, @Descripcion + @ListaSerie, @PrecioUnitario, 
				  @Descuento , @Importe, @Medida, @IndCorrelativo)
		SELECT @MovimientoDetId=@@IDENTITY
	END
	ELSE
	BEGIN
		UPDATE	ALMACEN.MovimientoDet
		SET		PrecioUnitario=@PrecioUnitario,
				Descuento = @Descuento,
				Cantidad = @Cantidad,
				Importe = @Importe,
				Descripcion = @Descripcion + @ListaSerie,
				IndCorrelativo = @IndCorrelativo,				
				UnidadMedidaT10 = @Medida
		WHERE	MovimientoDetId = @MovimientoDetId
	END
	
	DELETE FROM ALMACEN.SerieArticulo WHERE MovimientoDetEntId=@MovimientoDetId
	
					
	IF @IndAutogenerar=0 AND @IndCorrelativo=0
	BEGIN
		INSERT INTO ALMACEN.SerieArticulo
		(NumeroSerie,AlmacenId,ArticuloId,EstadoId,MovimientoDetEntId)
		SELECT	Name 'Serie',@AlmacenId,@ArticuloId,@EstadoSerie,@MovimientoDetId
		FROM	dbo.Split(@ListaSerie,',')
	END
	ELSE
	BEGIN
		DECLARE @SerieIni BIGINT, @SerieFin BIGINT, @Serie VARCHAR(20), @Limite INT
		IF @IndAutogenerar=1
		BEGIN
			SELECT @ListaSerie =CAST(MAX(CAST(NumeroSerie AS BIGINT) + 1) AS VARCHAR(20)) FROM ALMACEN.SerieArticulo
		END
		
		SET @Limite = LEN(@ListaSerie)
		IF @Limite > 9
			SET @Limite = 9
				
		SET @SerieIni = CAST(SUBSTRING(@ListaSerie, LEN(@ListaSerie)- @Limite, LEN(@ListaSerie)+1) AS BIGINT)
		SET @SerieFin = @SerieIni + @Cantidad - 1
		SET @Serie = SUBSTRING(@ListaSerie, 0,LEN(@ListaSerie)- @Limite) 
		
		WHILE(@SerieIni <= @SerieFin)	
		BEGIN
			INSERT INTO ALMACEN.SerieArticulo
			(NumeroSerie,AlmacenId,ArticuloId,EstadoId,MovimientoDetEntId)
			VALUES(	@Serie + CAST(@SerieIni AS VARCHAR(20)),@AlmacenId,@ArticuloId,@EstadoSerie,@MovimientoDetId)
			
			SET @SerieIni = @SerieIni + 1
		END
		
		IF @Cantidad>1
			UPDATE	ALMACEN.MovimientoDet 
			SET		Descripcion = @Descripcion + @ListaSerie + ' al ' + @Serie + CAST(@SerieFin AS VARCHAR(20))
			WHERE	MovimientoDetId=@MovimientoDetId
		ELSE
			UPDATE	ALMACEN.MovimientoDet 
			SET		Descripcion = @Descripcion + @ListaSerie 
			WHERE	MovimientoDetId=@MovimientoDetId
		
			
	END
	
	;WITH DETALLE AS (
		SELECT	MovimientoId,SUM(Importe) 'TotalImporte' 
		FROM	ALMACEN.MovimientoDet 
		WHERE	MovimientoId=@MovimientoId 
		GROUP	BY MovimientoId
	)
	UPDATE	M
	SET		M.SubTotal=D.TotalImporte / (1 + @IGV),
			M.IGV = D.TotalImporte - (D.TotalImporte / (1 + @IGV)),
			M.AjusteRedondeo = 0,
			M.TotalImporte = D.TotalImporte
	FROM	ALMACEN.Movimiento M
	INNER JOIN DETALLE D ON M.MovimientoId = D.MovimientoId 
	WHERE	M.MovimientoId=@MovimientoId
	--UPDATE	M
	--SET		M.SubTotal=D.TotalImporte,
	--		M.IGV = D.TotalImporte * @IGV,
	--		M.AjusteRedondeo = 0,
	--		M.TotalImporte = D.TotalImporte * ( 1 + @IGV )
	--FROM	ALMACEN.Movimiento M
	--INNER JOIN DETALLE D ON M.MovimientoId = D.MovimientoId 
	--WHERE	M.MovimientoId=@MovimientoId
	
END
