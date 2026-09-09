
/*
ALMACEN.usp_GenerarKardex @ArticuloId = 534,  @AlmacenId = 9
*/

CREATE PROC [ALMACEN].[usp_GenerarKardex]
@ArticuloId INT,	
@AlmacenId INT
--@AFecha DATE=NULL,
--@IndCierre BIT = 0
AS

DECLARE	@tmpKardex TABLE(Fila INT,MovimientoDetId INT,Fecha DATETIME,IndEntrada BIT,Concepto VARCHAR(MAX),
		CantEnt INT,PUEnt DECIMAL(16,2),TotalEnt DECIMAL(16,2),
		CantSal INT,PUSal DECIMAL(16,2) ,TotalSal DECIMAL(16,2) ,
		CantSaldo INT,PUSaldo DECIMAL(16,2),TotalSaldo DECIMAL(16,2))
			
;WITH DATOS AS(
	SELECT	MD.MovimientoDetId,M.Fecha, 1 'IndEntrada',MD.PrecioUnitario,M.TipoMovimientoId
	FROM	ALMACEN.SerieArticulo SA
	INNER JOIN ALMACEN.MovimientoDet MD ON SA.MovimientoDetEntId = MD.MovimientoDetId
	INNER JOIN ALMACEN.Movimiento M ON MD.MovimientoId = M.MovimientoId
	--INNER JOIN ALMACEN.TipoMovimiento TM ON M.TipoMovimientoId = TM.TipoMovimientoId
	WHERE SA.EstadoId IN(2,3,4) AND SA.ArticuloId=@ArticuloId AND SA.AlmacenId=@AlmacenId
	UNION ALL
	SELECT	MD.MovimientoDetId,M.Fecha,0 'IndEntrada',MD.PrecioUnitario,M.TipoMovimientoId
	FROM ALMACEN.SerieArticulo SA
	INNER JOIN ALMACEN.MovimientoDet MD ON SA.MovimientoDetSalId = MD.MovimientoDetId
	INNER JOIN ALMACEN.Movimiento M ON MD.MovimientoId = M.MovimientoId
	--INNER JOIN ALMACEN.TipoMovimiento TM ON M.TipoMovimientoId = TM.TipoMovimientoId
	WHERE SA.EstadoId = 4 AND SA.ArticuloId=@ArticuloId AND SA.AlmacenId=@AlmacenId
),CANTIDAD AS(
	SELECT	ROW_NUMBER() OVER(ORDER BY Fecha ASC) AS 'Fila',MovimientoDetId,Fecha,IndEntrada,TipoMovimientoId,PrecioUnitario,COUNT(1) 'Cantidad'
		   --STUFF((SELECT ',' + rtrim(convert(char(10),NumeroSerie))
		--   FROM   DATOS b WHERE  a.Fecha = b.Fecha AND A.IndEntrada=b.IndEntrada
		--   FOR XML PATH('')),1,1,'') 'Codigos'
	FROM DATOS A
	GROUP BY MovimientoDetId,Fecha,IndEntrada,TipoMovimientoId,PrecioUnitario
)
INSERT INTO @tmpKardex(Fila,MovimientoDetId,Fecha,IndEntrada,Concepto,CantEnt,PUEnt,TotalEnt,CantSal)
SELECT	Fila,MovimientoDetId,Fecha,C.IndEntrada,TM.Descripcion,
		CASE WHEN C.IndEntrada=1 THEN Cantidad ELSE NULL END 'CantEnt',
		CASE WHEN C.IndEntrada=1 THEN PrecioUnitario ELSE NULL END 'PUEnt',
		CASE WHEN C.IndEntrada=1 THEN PrecioUnitario*Cantidad ELSE NULL END 'TotalEnt',
		CASE WHEN C.IndEntrada=0 THEN Cantidad ELSE NULL END 'CantSal'
FROM CANTIDAD C
INNER JOIN ALMACEN.TipoMovimiento TM ON C.TipoMovimientoId = TM.TipoMovimientoId



DECLARE @CantSaldoAnt INT=0, @PUSaldoAnt DECIMAL(16,2)=0, @TotalSaldoAnt DECIMAL(16,2)=0
DECLARE @Sec INT=1, @Limite INT =(select COUNT(1) from @tmpKardex), @IndEntrada BIT
WHILE (@Sec<=@Limite)
BEGIN 
	SELECT	@IndEntrada = IndEntrada FROM @tmpKardex WHERE Fila=@Sec
	
	IF @Sec>1	
	BEGIN
		SELECT	@CantSaldoAnt = CantSaldo, @PUSaldoAnt=PUSaldo, @TotalSaldoAnt=TotalSaldo
		FROM @tmpKardex WHERE Fila=@Sec-1
	END
	
	IF @IndEntrada=1
	BEGIN		
		UPDATE @tmpKardex 
		SET CantSaldo = @CantSaldoAnt + CantEnt,
			TotalSaldo = @TotalSaldoAnt + TotalEnt , 
			PUSaldo=CASE WHEN (@CantSaldoAnt + CantEnt)= 0 THEN 0 
								ELSE ((@TotalSaldoAnt + TotalEnt)/(@CantSaldoAnt + CantEnt)) 
					END
		WHERE Fila=@Sec
	END
	ELSE
	BEGIN
		UPDATE @tmpKardex 
		SET PUSal = @PUSaldoAnt,
			TotalSal = @PUSaldoAnt * CantSal,
			CantSaldo = @CantSaldoAnt - CantSal,
			TotalSaldo = @TotalSaldoAnt - (@PUSaldoAnt * CantSal) , 
			PUSaldo=CASE WHEN (@CantSaldoAnt - CantSal)= 0 THEN 0 
								ELSE ((@TotalSaldoAnt - (@PUSaldoAnt * CantSal))/(@CantSaldoAnt - CantSal)) 
					END
		WHERE Fila=@Sec
	END		
	
	SET @Sec=@Sec+1
END

SELECT	MovimientoDetId,CAST(Fecha AS DATE) 'Fecha',Concepto,
		CantEnt,PUEnt,TotalEnt,CantSal,PUSal,TotalSal,CantSaldo,PUSaldo,TotalSaldo
FROM @tmpKardex
