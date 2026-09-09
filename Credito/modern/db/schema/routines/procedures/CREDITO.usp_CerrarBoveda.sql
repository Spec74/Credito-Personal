/*
SELECT * FROM CREDITO.BovedaCuenta ORDER BY 1 DESC
SELECT * FROM CREDITO.Boveda ORDER BY 1 DESC

DELETE	CREDITO.BovedaCuenta WHERE BovedaCuentaId>13
DELETE CREDITO.Boveda WHERE BovedaId= 6742
UPDATE CREDITO.Boveda SET IndCierre=0 WHERE BovedaId=6737

CREDITO.usp_CerrarBoveda  1, 3
*/

CREATE PROCEDURE [CREDITO].[usp_CerrarBoveda]
    @OficinaId INT,
	@UsuarioRegId INT
AS
DECLARE @BovedaId INT = (SELECT BovedaId FROM CREDITO.Boveda WHERE OficinaId=@OficinaId AND	IndTemporal=0 AND IndCierre=0)
DECLARE @BovedaNuevoId INT
EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId
DECLARE @SaldoFinal DECIMAL(15,2) = (SELECT SaldoFinal FROM CREDITO.Boveda WHERE BovedaId=@BovedaId)

UPDATE CREDITO.Boveda
SET IndCierre=1, FechaFinOperacion=dbo.ufnFecha()
WHERE BovedaId= @BovedaId

INSERT INTO CREDITO.Boveda
(OficinaId, SaldoInicial, Entradas, Salidas, SaldoFinal, FechaIniOperacion, FechaFinOperacion, IndCierre, IndTemporal)
VALUES (@OficinaId,@SaldoFinal, 0, 0, @SaldoFinal, dbo.ufnFecha(), NULL,0, 0 )

SELECT @BovedaNuevoId=@@IDENTITY

	;WITH CUENTAS AS (
		SELECT MC.BovedaId,MC.TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.Importe,-MC.Importe) ) 'Importe'
		FROM CREDITO.BovedaMov MC  
		WHERE MC.BovedaId=@BovedaId 
		AND MC.Estado=1	
		GROUP BY MC.BovedaId,MC.TipoPagoId	
	)
	INSERT	INTO CREDITO.BovedaCuenta
	(BovedaId,TipoPagoId,SaldoInicial)
	SELECT @BovedaNuevoId,vt.ItemId, ISNULL(BC.SaldoInicial,0) + ISNULL(c.Importe,0) 'SaldoFinal'	
	FROM MAESTRO.ValorTabla vt
	LEFT JOIN CUENTAS C ON  vt.ItemId=c.TipoPagoId
	LEFT JOIN CREDITO.BovedaCuenta BC ON BC.BovedaId = @BovedaId and BC.TipoPagoId = vt.ItemId
	WHERE vt.TablaId=13 AND vt.ItemId>0

	DELETE CREDITO.BovedaCuenta WHERE BovedaId=@BovedaNuevoId AND SaldoInicial=0
