-- VENTAS.usp_CodigoBarras_Lst 72
CREATE PROC [VENTAS].[usp_CodigoBarras_Lst]
@pMovimientoId INT=0
AS

WITH LISTA AS(
	SELECT	ROW_NUMBER() OVER(ORDER BY SerieArticuloId ASC) AS Fila,
			SA.SerieArticuloId,SA.NumeroSerie 'Serie',A.Denominacion 'Articulo',
			ISNULL(LP.Monto,0.0) 'Precio'
	FROM ALMACEN.MovimientoDet MD
	INNER JOIN ALMACEN.SerieArticulo SA ON MD.MovimientoDetId = SA.MovimientoDetEntId
	INNER JOIN ALMACEN.Articulo A ON SA.ArticuloId = A.ArticuloId
	LEFT JOIN VENTAS.ListaPrecio LP ON A.ArticuloId = LP.ArticuloId AND LP.Estado=1
	WHERE MD.MovimientoId=@pMovimientoId
),CAMPO1 AS (
	SELECT ROW_NUMBER() OVER(ORDER BY SerieArticuloId ASC) AS Row,* 
	FROM LISTA
	WHERE (Fila % 2) <> 0
),CAMPO2 AS (
	SELECT ROW_NUMBER() OVER(ORDER BY SerieArticuloId ASC) AS Row,* 
	FROM LISTA
	WHERE (Fila % 2) = 0
)
SELECT	'*' + C1.Serie + '*' 'Serie1',C1.Articulo 'Articulo1', C1.Precio 'Precio1',
		'*' + ISNULL(C2.Serie,C1.Serie) + '*' 'Serie2',ISNULL(C2.Articulo,C1.Articulo) 'Articulo2', ISNULL(C2.Precio,C1.Precio) 'Precio2'
FROM CAMPO1 C1
LEFT JOIN CAMPO2 C2 ON C1.Row = C2.Row
