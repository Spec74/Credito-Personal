
/*
declare @retorno varchar(50)
exec ALMACEN.usp_ExisteSerieArticulo @ListaSerie = '120',@IndCorrelativo = 0,@Cantidad = 10,
		@Retorno=@retorno OUTPUT				
select @retorno
*/
CREATE PROC [ALMACEN].[usp_ExisteSerieArticulo]
	@ListaSerie VARCHAR(MAX),
	@Cantidad INT,
	@IndCorrelativo BIT=0--,
	--@Retorno VARCHAR(50) OUTPUT
AS
BEGIN
	
DECLARE @lstExiste VARCHAR(50), @SerieAnulado INT
SET @SerieAnulado = 4

IF @IndCorrelativo=0
	BEGIN
		SELECT	@lstExiste =  ISNULL(@lstExiste+',','') + Name 
		FROM	dbo.Split(@ListaSerie,',') S
		INNER JOIN ALMACEN.SerieArticulo SA ON SA.NumeroSerie=S.Name AND SA.EstadoId<>@SerieAnulado		
	END
ELSE
	BEGIN
		DECLARE @tserie TABLE (NumeroSerie VARCHAR(20))
		DECLARE @SerieIni BIGINT, @SerieFin BIGINT, @Serie VARCHAR(20), @Limite INT
		SET @Limite = LEN(@ListaSerie)
		IF @Limite > 9
			SET @Limite = 9
				
		SET @SerieIni = CAST(SUBSTRING(@ListaSerie, LEN(@ListaSerie)- @Limite, LEN(@ListaSerie)+1) AS BIGINT)
		SET @SerieFin = @SerieIni + @Cantidad - 1
		SET @Serie = SUBSTRING(@ListaSerie, 0,LEN(@ListaSerie)- @Limite) 
		
		WHILE(@SerieIni <= @SerieFin)	
		BEGIN
			INSERT INTO @tserie VALUES(@Serie + CAST(@SerieIni AS VARCHAR(20)))
			SET @SerieIni+=1
		END
				
		SELECT	@lstExiste =  ISNULL(@lstExiste+',','') + S.NumeroSerie 
		FROM	@tserie S
		INNER JOIN ALMACEN.SerieArticulo SA ON SA.NumeroSerie=S.NumeroSerie AND SA.EstadoId<>@SerieAnulado
		
	END		
		
	--SET @Retorno = ISNULL(@lstExiste,'') 		
	SELECT ISNULL(@lstExiste,'') 'Existe'		
		
END
