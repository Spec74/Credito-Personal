
 -- EXEC [ALMACEN].[usp_ReporteStock] 1
CREATE PROC [ALMACEN].[usp_ReporteStock]
@OficinaId INT
AS
;WITH STOCK AS(
	SELECT SA.ArticuloId,COUNT(1) 'Stock' 
	FROM ALMACEN.SerieArticulo SA
	INNER JOIN ALMACEN.Almacen A ON SA.AlmacenId = A.AlmacenId
	WHERE EstadoId=2 AND A.OficinaId = ISNULL(@OficinaId,A.OficinaId)
	GROUP BY SA.ArticuloId
	HAVING COUNT(1)>0
)
SELECT	ROW_NUMBER() OVER(ORDER BY A.TipoArticuloId , A.Denominacion) 'Nro',
		TA.Denominacion 'TipoArticulo',A.ArticuloId,A.Denominacion 'Articulo', ISNULL(S.Stock,0) 'Stock',
		LTRIM(
			STUFF((SELECT ', ' + RTRIM(convert(char(15),NumeroSerie))
			FROM	ALMACEN.SerieArticulo b 
			INNER JOIN ALMACEN.Almacen AL ON  b.AlmacenId = AL.AlmacenId
			WHERE	b.ArticuloId=A.ArticuloId AND AL.OficinaId=ISNULL(@OficinaId,AL.OficinaId) AND b.EstadoId=2
			FOR XML PATH('')),1,1,'')
		) 'Series'
FROM ALMACEN.Articulo A
INNER JOIN STOCK S ON A.ArticuloId=S.ArticuloId
INNER JOIN ALMACEN.TipoArticulo TA ON A.TipoArticuloId = TA.TipoArticuloId
ORDER BY A.TipoArticuloId , A.Denominacion
