-- CREDITO.ActualizarClientesNuevos 8163
CREATE	PROC CREDITO.ActualizarClientesNuevos
@CajaDiarioId INT
AS

DECLARE @UsuarioAsignadoId INT = (SELECT UsuarioAsignadoId FROM CREDITO.CajaDiario WHERE CajaDiarioId=@CajaDiarioId)
DECLARE @Fecha DATE = dbo.ufnFecha()
DECLARE @FechaInicio DATE,@FechaFin DATE
DECLARE @ClientesNuevos INT = 0
SET @FechaInicio =DATEADD(DAY,-DAY(@Fecha) + 1,@Fecha)    
SET @FechaFin = DATEADD(DAY,-1,DATEADD(MONTH,1,@FechaInicio)) 

;WITH NUEVOS AS (
	SELECT DISTINCT C.PersonaId 
	FROM CREDITO.Credito C
	INNER JOIN MAESTRO.Cliente CL  ON CL.PersonaId = C.PersonaId
	WHERE CAST( CL.FechaRegistro AS DATE) BETWEEN @FechaInicio AND @FechaFin
	AND C.UsuarioRegId = @UsuarioAsignadoId
)
SELECT @ClientesNuevos=COUNT(1)
FROM NUEVOS

UPDATE CREDITO.CajaDiario  
SET NroClientesNuevos = @ClientesNuevos
WHERE CajaDiarioId=@CajaDiarioId
