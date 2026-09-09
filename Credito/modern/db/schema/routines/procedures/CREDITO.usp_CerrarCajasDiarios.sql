-- EXEC CREDITO.usp_CerrarCajasDiarios 1025


CREATE PROC [CREDITO].[usp_CerrarCajasDiarios]
@UsuarioCierreId INT ,
@OficinaId INT = 1,
@Sobrante DECIMAL(15,2)=0
AS
DECLARE @Fecha DATETIME = dbo.ufnFecha()
DECLARE @FechaTexto VARCHAR(20) = 'CIERRE ' + CONVERT(varchar,@Fecha,103) + ' '
DECLARE @BovedaId INT = (SELECT BovedaId FROM CREDITO.Boveda WHERE OficinaId=@OficinaId AND IndCierre=0 AND IndTemporal=0)

IF EXISTS(SELECT 1 FROM CREDITO.Boveda WHERE OficinaId=@OficinaId AND IndCierre=0 AND IndTemporal=1)
	SET @BovedaId = (SELECT BovedaId FROM CREDITO.Boveda WHERE OficinaId=@OficinaId AND IndCierre=0 AND IndTemporal=1)


;WITH MOVS AS (
   SELECT C.Denominacion 'Caja', CD.CajaDiarioId, TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.ImportePago,-MC.ImportePago) ) 'Importe'
	FROM CREDITO.MovimientoCaja MC
	INNER JOIN CREDITO.CajaDiario CD ON CD.CajaDiarioId = MC.CajaDiarioId
	INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
	WHERE CD.IndCierre=1 AND cd.TransBoveda=0  AND C.OficinaId=1
	AND MC.Estado=1	
	GROUP BY C.Denominacion, CD.CajaDiarioId,MC.TipoPagoId	
),
CUENTAS AS(
	SELECT  M.CajaDiarioId,M.TipoPagoId,M.Caja, IIF(M.TipoPagoId=1,CD.SaldoInicial + M.Importe,M.Importe)  'Importe' 
	FROM MOVS M
	INNER JOIN CREDITO.CajaDiario CD ON CD.CajaDiarioId = M.CajaDiarioId
)
INSERT INTO	CREDITO.BovedaMov
(
    BovedaId, CodOperacion,Glosa,
    CajaDiarioId,TipoPagoId,Importe,
    IndEntrada,Estado,UsuarioRegId,FechaReg
)
SELECT @BovedaId ,'TRE' , @FechaTexto + CU.Caja , 
		CU.CajaDiarioId, CU.TipoPagoId, ROUND(CU.Importe,1)  , 
		1 , 1 , @UsuarioCierreId , @Fecha 
FROM CUENTAS CU
ORDER BY CU.CajaDiarioId, CU.TipoPagoId

IF @Sobrante>0
	INSERT INTO	CREDITO.BovedaMov
	(
		BovedaId, CodOperacion,Glosa,
		CajaDiarioId,TipoPagoId,Importe,
		IndEntrada,Estado,UsuarioRegId,FechaReg
	)
	VALUES( @BovedaId ,'TRE'  , @FechaTexto + 'SOBRANTE' , 
			0, 1 , @Sobrante, 
			1 , 1 , @UsuarioCierreId , @Fecha  )

UPDATE CD
SET CD.TransBoveda=1
--SELECT  CD.CajaDiarioId
FROM CREDITO.CajaDiario CD
INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
WHERE CD.IndCierre=1 AND cd.TransBoveda=0  AND C.OficinaId=@OficinaId

EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId 
--EXEC CREDITO.usp_ActualizarSaldoCartera @OficinaId = @OficinaId 
--EXEC CREDITO.usp_CalificarCliente @OficinaId = @OficinaId 
/*
UPDATE CREDITO.CajaDiario  SET TransBoveda=0 WHERE CajaDiarioId IN(19030,19029)
DELETE FROM CREDITO.BovedaMov WHERE MovimientoBovedaId>46998
*/
