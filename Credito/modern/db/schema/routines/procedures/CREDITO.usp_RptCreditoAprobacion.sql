/*

EXEC CREDITO.usp_RptCreditoAprobacion '20191106'

*/
CREATE PROC CREDITO.usp_RptCreditoAprobacion
@FechaAprobacion DATE ,
@UsuarioId INT = NULL,
@OficinaId INT = NULL
AS


	SELECT	C.CreditoId,O.Denominacion 'Oficina', P.NombreCompleto 'Cliente',FechaAprobacion,
			C.MontoCredito,Interes,NumeroCuotas,MontoDesembolso, G.NombreCompleto 'Gestor'		
	FROM CREDITO.Credito C
	INNER JOIN MAESTRO.Persona P ON C.PersonaId = P.PersonaId
	INNER JOIN MAESTRO.Oficina O ON C.OficinaId = O.OficinaId
	INNER JOIN MAESTRO.Usuario U ON C.UsuarioRegId = U.UsuarioId
	INNER JOIN MAESTRO.Persona G ON U.PersonaId = G.PersonaId	
	WHERE	(C.Estado ='APR' OR C.Estado='DES' OR C.Estado='PAG') AND
			C.OficinaId = ISNULL(@OficinaId,C.OficinaId)  AND
			C.UsuarioRegId = ISNULL(@UsuarioId,C.UsuarioRegId) AND
			CAST(C.FechaAprobacion AS DATE) = @FechaAprobacion
