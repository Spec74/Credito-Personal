

--DECLARE @tem TABLE(Capital DECIMAL(18,16))
--INSERT INTO @tem
--exec [CREDITO].[usp_CalcularTEM] 19.2,'M'
--select * from @tem

/*
exec [CREDITO].[usp_CalcularTEM] 19.2,'M'
*/

CREATE PROC [CREDITO].[usp_CalcularTEM] ( @TEA DECIMAL(4,2) , @FormaPago CHAR(1) )
AS
DECLARE @TEM DECIMAL(18,16) 
DECLARE @PeriodoAnio INT= CASE @FormaPago WHEN 'M' THEN 12 WHEN 'Q' THEN 24 WHEN 'S' THEN 52 WHEN 'D' THEN 360 END
SET @TEM = (POWER(CAST(1+(@TEA/100) AS FLOAT),CAST(1.0/@PeriodoAnio AS FLOAT)))-1

SELECT @TEM 'TEM'
