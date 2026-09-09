
CREATE PROCEDURE [dbo].[usp_CanjearPuntos]
@CodCliente NUMERIC(9,0),
@NumeroSerie varchar(20)

AS
DECLARE @NumPuntos	int , @TotalPuntos int, @Mensaje VARCHAR(150), @ArticuloId int,@EstadoEnAlmacen int,
		@Descripcion varchar(150) 
SET @NumPuntos = 0
SET @Mensaje = ''
SET @EstadoEnAlmacen = 2

BEGIN

IF EXISTS(SELECT 1 FROM ALMACEN.SerieArticulo sa WHERE sa.NumeroSerie = @NumeroSerie AND sa.EstadoId = 2) 
BEGIN
	SELECT  @ArticuloId = sa.ArticuloId FROM ALMACEN.SerieArticulo sa WHERE sa.NumeroSerie = @NumeroSerie
	SELECT  @NumPuntos = PuntosCanje FROM VENTAS.ListaPrecio WHERE ArticuloId = @ArticuloId
	SELECT  @TotalPuntos = TotalPuntos FROM VENTAS.TarjetaPunto --WHERE CodCliente = @CodCliente

	IF NOT EXISTS( SELECT	1 FROM	ALMACEN.SerieArticulo SA
						INNER	JOIN ALMACEN.Articulo A ON SA.ArticuloId = A.ArticuloId AND A.Estado=1
						INNER	JOIN VENTAS.ListaPrecio LP ON A.ArticuloId = LP.ArticuloId AND A.Estado=1
						WHERE	NumeroSerie=@NumeroSerie AND SA.EstadoId=@EstadoEnAlmacen)
		SELECT	@Mensaje='No existe Lista de Precio para el artículo ' + A.Denominacion
		FROM	ALMACEN.SerieArticulo SA
		INNER	JOIN ALMACEN.Articulo A ON SA.ArticuloId = A.ArticuloId
		WHERE	NumeroSerie = @NumeroSerie
	ELSE	
	BEGIN	
	IF(@TotalPuntos >= @NumPuntos)
		BEGIN
		UPDATE VENTAS.TarjetaPunto SET TotalPuntos = TotalPuntos - @NumPuntos --WHERE CodCliente = @CodCliente
	
		--RealizarCanje
		SELECT @Descripcion = a.Denominacion + ' SN: ' + @NumeroSerie  FROM ALMACEN.Articulo a WHERE a.ArticuloId = @ArticuloId
		
		UPDATE ALMACEN.SerieArticulo SET EstadoId = 3 WHERE NumeroSerie = @NumeroSerie
		END
	ELSE
		BEGIN
		SET @Mensaje = 'No tiene Puntos Suficientes'
		END
	END
END
ELSE
	BEGIN
		SELECT	@Mensaje='El articulo ' + A.Denominacion + ' se encuentra en estado ' + E.Denominacion
		FROM	ALMACEN.SerieArticulo SA
		INNER	JOIN ALMACEN.Articulo A ON SA.ArticuloId = A.ArticuloId
		INNER	JOIN MAESTRO.ValorTabla E ON E.TablaId=6 AND E.ItemId = SA.EstadoId
		WHERE	NumeroSerie=@NumeroSerie 

		IF NOT EXISTS(SELECT 1 FROM ALMACEN.SerieArticulo WHERE NumeroSerie=@NumeroSerie)
		SET @Mensaje='No existe Artículo !!!'

	END

SELECT @Mensaje
END
