
/*
EXEC ALMACEN.usp_ListarSerieKardex 1612
*/
CREATE PROC [ALMACEN].[usp_ListarSerieKardex]
@MovimientoDetalleId INT,
@IndStock BIT=0
AS
DECLARE @AlmacenId INT,@ArticuloId INT,@IndEntrada BIT,@Fecha DATETIME

SELECT @AlmacenId=M.AlmacenId, @ArticuloId=MD.ArticuloId,@IndEntrada=TM.IndEntrada,@Fecha=M.Fecha
FROM ALMACEN.MovimientoDet MD
INNER JOIN ALMACEN.Movimiento M ON MD.MovimientoId = M.MovimientoId
INNER JOIN ALMACEN.TipoMovimiento TM ON M.TipoMovimientoId = TM.TipoMovimientoId
WHERE MovimientoDetId=@MovimientoDetalleId

IF @IndStock=1
BEGIN
	SELECT	STUFF((SELECT ', ' + rtrim(convert(char(15),NumeroSerie))
	FROM	ALMACEN.SerieArticulo b 
	INNER JOIN ALMACEN.MovimientoDet MD ON b.MovimientoDetEntId = MD.MovimientoDetId
	INNER JOIN ALMACEN.Movimiento M ON MD.MovimientoId = M.MovimientoId
	LEFT JOIN ALMACEN.MovimientoDet MDS ON b.MovimientoDetSalId = MDS.MovimientoDetId
	LEFT JOIN ALMACEN.Movimiento MS ON MDS.MovimientoId = MS.MovimientoId
	WHERE	b.ArticuloId=@ArticuloId AND b.AlmacenId=@AlmacenId
	AND ((b.EstadoId IN(2,3) AND M.Fecha<=@Fecha)  OR
			(b.EstadoId =4 AND M.Fecha<=@Fecha AND MS.Fecha>@Fecha))
	FOR XML PATH('')),1,1,'') 'Series'
	
END
ELSE
BEGIN
	IF @IndEntrada=1
	BEGIN
		SELECT	STUFF((SELECT ', ' + rtrim(convert(char(15),NumeroSerie))
		FROM	ALMACEN.SerieArticulo b 
		WHERE	ArticuloId=@ArticuloId AND AlmacenId=@AlmacenId AND MovimientoDetEntId=@MovimientoDetalleId
		FOR XML PATH('')),1,1,'') 'Series'	
	END
	ELSE
	BEGIN
		SELECT	STUFF((SELECT ', ' + rtrim(convert(char(15),NumeroSerie))
		FROM	ALMACEN.SerieArticulo b 
		WHERE	ArticuloId=@ArticuloId AND AlmacenId=@AlmacenId AND MovimientoDetSalId=@MovimientoDetalleId
		FOR XML PATH('')),1,1,'') 'Series'	
	END	
	
END
