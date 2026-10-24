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

IF @BovedaIdTemporal IS NULL
BEGIN
	RAISERROR(N'No hay bóveda temporal abierta en la oficina.', 16, 1)
	RETURN
END

IF @BovedaId IS NULL
BEGIN
	RAISERROR(N'No hay bóveda principal abierta para devolver el saldo temporal.', 16, 1)
	RETURN
END

EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaIdTemporal

;WITH CUENTAS AS (
	SELECT MC.BovedaId,TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.Importe,-MC.Importe) ) 'Importe'
	FROM CREDITO.BovedaMov MC
	WHERE MC.BovedaId=@BovedaIdTemporal
	AND MC.Estado=1
	GROUP BY MC.BovedaId,MC.TipoPagoId
)
INSERT	INTO CREDITO.BovedaMov(BovedaId,CodOperacion,Glosa,TipoPagoId,Importe,IndEntrada, Estado, CajaDiarioId, UsuarioRegId, FechaReg)
SELECT @BovedaId,'TRE','CIERRE BOVEDA TEMPORAL',vt.ItemId, ISNULL(BC.SaldoInicial,0) + ISNULL(c.Importe,0),1,1,0,@UsuarioRegId,dbo.ufnFecha()
FROM MAESTRO.ValorTabla vt
LEFT JOIN CUENTAS C ON vt.ItemId = C.TipoPagoId
LEFT JOIN CREDITO.BovedaCuenta BC ON BC.BovedaId = @BovedaIdTemporal AND BC.TipoPagoId = vt.ItemId
WHERE vt.TablaId=13 AND vt.ItemId BETWEEN 1 AND 5
AND ISNULL(BC.SaldoInicial,0) + ISNULL(c.Importe,0) <> 0

UPDATE CREDITO.Boveda
SET IndCierre=1, FechaFinOperacion=dbo.ufnFecha()
WHERE BovedaId= @BovedaIdTemporal

EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId

SELECT @RolEncargado=r.RolId
FROM MAESTRO.Rol AS r
WHERE r.Denominacion ='ENCARGADO'

IF @RolEncargado IS NOT NULL
	DELETE FROM MAESTRO.UsuarioRol
	WHERE RolId=@RolEncargado AND OficinaId=@OficinaId
