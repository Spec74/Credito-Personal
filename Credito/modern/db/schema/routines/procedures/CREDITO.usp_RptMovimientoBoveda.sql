
-- SELECT * FROM CREDITO.BovedaMov
-- exec CREDITO.usp_RptMovimientoBoveda 55

CREATE PROC [CREDITO].[usp_RptMovimientoBoveda]
@BovedaId INT=NULL
AS
IF	@BovedaId IS NULL
	SELECT @BovedaId=BovedaId FROM CREDITO.Boveda WHERE IndCierre=0

	
SELECT M.MovimientoBovedaId,M.FechaReg,M.CodOperacion, M.Glosa, 
		CASE WHEN M.IndEntrada=1 THEN M.Importe ELSE NULL END 'Entrada',
		CASE WHEN M.IndEntrada=0 THEN M.Importe ELSE NULL END 'Salida',
		VT.Denominacion 'TipoPago',
		ISNULL(P.NombreCompleto,'') 'Agente'
FROM CREDITO.BovedaMov M
LEFT JOIN CREDITO.CajaDiario c ON M.CajaDiarioId=c.CajaDiarioId
LEFT JOIN MAESTRO.Usuario U ON C.UsuarioAsignadoId=U.UsuarioId
LEFT JOIN MAESTRO.Persona P ON U.PersonaId=P.PersonaId
LEFT JOIN MAESTRO.ValorTabla VT ON VT.TablaId=13 AND VT.ItemId=M.TipoPagoId
WHERE M.Estado=1 AND M.BovedaId= @BovedaId
ORDER BY M.FechaReg
