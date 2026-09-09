
--CREDITO.usp_CerrarBovedaTemporal  1, 3
CREATE PROCEDURE [CREDITO].[usp_CerrarBovedaTemporal]
    @OficinaId INT,
	@UsuarioRegId INT
AS
DECLARE @BovedaId INT, @BovedaIdTemporal INT, @ImporteBovedaTemporal DECIMAL(15,2), @RolEncargado INT

SELECT @BovedaIdTemporal=BovedaId, @ImporteBovedaTemporal=SaldoFinal 
FROM CREDITO.Boveda 
WHERE OficinaId=@OficinaId AND	IndTemporal=1 AND IndCierre=0

SELECT @BovedaId=BovedaId 
FROM CREDITO.Boveda 
WHERE OficinaId=@OficinaId AND	IndTemporal=0 AND IndCierre=0

IF @BovedaIdTemporal IS NOT NULL
BEGIN	
	
	;WITH CUENTAS AS (
		SELECT MC.BovedaId,TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.Importe,-MC.Importe) ) 'Importe'
		FROM CREDITO.BovedaMov MC  
		WHERE MC.BovedaId=@BovedaIdTemporal 
		AND MC.Estado=1	
		GROUP BY MC.BovedaId,MC.TipoPagoId	
	)
	INSERT	INTO CREDITO.BovedaMov(BovedaId,CodOperacion,Glosa,TipoPagoId,Importe,IndEntrada, Estado, CajaDiarioId, UsuarioRegId, FechaReg)
	SELECT @BovedaId,'TRE','CIERRE BOVEDA TEMPORAL',C.TipoPagoId, ISNULL(BC.SaldoInicial,0) + c.Importe 'SaldoFinal',1,1,0,@UsuarioRegId,dbo.ufnFecha()
	FROM CUENTAS C	
	LEFT JOIN CREDITO.BovedaCuenta BC ON BC.BovedaId = C.BovedaId and BC.TipoPagoId = C.TipoPagoId
	

	UPDATE CREDITO.Boveda
	SET IndCierre=1, FechaFinOperacion=dbo.ufnFecha()
	WHERE BovedaId= @BovedaIdTemporal
		
	EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId

	SELECT @RolEncargado=r.RolId 
	FROM MAESTRO.Rol AS r 
	WHERE r.Denominacion ='ENCARGADO'

	DELETE FROM MAESTRO.UsuarioRol WHERE RolId=@RolEncargado
END
