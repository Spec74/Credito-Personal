
CREATE PROC [ALMACEN].[usp_EliminarMovimientoDet]
@MovimientoDetId INT
AS
BEGIN
	--DECLARE @tserie TABLE (SerieArticuloId int)

	--INSERT INTO @tserie
	--SELECT	SerieArticuloId 
	--FROM	ALMACEN.MovimientoDetSerie 
	--WHERE	MovimientoDetId=@MovimientoDetId
	
	DECLARE @MovimientoId INT, @IGV DECIMAL(16,2)
	SELECT @MovimientoId = MovimientoId FROM ALMACEN.MovimientoDet WHERE MovimientoDetId=@MovimientoDetId
	SET @IGV = 0.18
	
	DELETE FROM ALMACEN.SerieArticulo 
	WHERE MovimientoDetEntId = @MovimientoDetId
	
	DELETE FROM ALMACEN.MovimientoDet 
	WHERE MovimientoDetId=@MovimientoDetId

	;WITH DETALLE AS (
		SELECT	MovimientoId,SUM(Importe) 'TotalImporte' 
		FROM	ALMACEN.MovimientoDet 
		WHERE	MovimientoId=@MovimientoId 
		GROUP	BY MovimientoId
	)
	UPDATE	M
	SET		M.SubTotal=D.TotalImporte,
			M.IGV = D.TotalImporte * @IGV,
			M.AjusteRedondeo = 0,
			M.TotalImporte = D.TotalImporte * ( 1 + @IGV )
	FROM	ALMACEN.Movimiento M
	INNER JOIN DETALLE D ON M.MovimientoId = D.MovimientoId 
	WHERE	M.MovimientoId=@MovimientoId
	
END
