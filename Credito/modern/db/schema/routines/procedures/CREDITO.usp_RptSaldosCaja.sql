-- EXEC CREDITO.usp_RptSaldosCaja 13728  
  
ALTER PROC [CREDITO].[usp_RptSaldosCaja]  
@CajaDiarioId INT,
@IndCajaChica BIT = 0
AS  

IF @IndCajaChica=0 BEGIN  
	SELECT MC.MovimientoCajaId, MC.Operacion, MC.FechaReg,P.Codigo, P.NombreCompleto 'Cliente', ImportePago, IndEntrada,  
	   MC.Descripcion 'Glosa' , ISNULL(TP.Denominacion,'') 'TipoPago'
	FROM CREDITO.MovimientoCaja MC  
	LEFT JOIN MAESTRO.VALORTABLA TP ON TP.TablaId=13 AND MC.TipoPagoId=TP.ItemId AND TP.ItemId>0
	LEFT JOIN MAESTRO.Persona P ON MC.PersonaId = P.PersonaId  
	WHERE MC.CajaDiarioId=@CajaDiarioId AND MC.Estado=1   
	ORDER BY FechaReg   	
	END
ELSE
BEGIN
	SELECT MC.Id 'MovimientoCajaId', MC.Operacion, MC.FechaReg,P.Codigo, P.NombreCompleto 'Cliente', MC.Importe 'ImportePago', IndEntrada,  
	   MC.Descripcion  'Glosa', 'EFECTIVO' 'TipoPago'  
	FROM CREDITO.MovimientoCajaChica MC 
	LEFT JOIN MAESTRO.Persona P ON MC.PersonaId = P.PersonaId  
	WHERE MC.CajaChicaDiarioId=@CajaDiarioId AND MC.Estado=1   
	ORDER BY FechaReg   	
END
