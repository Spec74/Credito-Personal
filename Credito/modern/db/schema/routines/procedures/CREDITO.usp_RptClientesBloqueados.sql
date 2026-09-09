
-- CREDITO.usp_RptClientesBloqueados 
CREATE PROC [CREDITO].[usp_RptClientesBloqueados]
@OficinaId INT = NULL,
@UsuarioId INT = NULL
AS

--DECLARE @OficinaId INT =1
--DECLARE @UsuarioId INT =NULL--1030

;WITH ClientesBase AS(
		SELECT DISTINCT C.PersonaId
		FROM CREDITO.Credito C
		WHERE C.OficinaId = ISNULL(@OficinaId,C.OficinaId)  AND
			  C.UsuarioRegId = ISNULL(@UsuarioId,C.UsuarioRegId) 
	)
	SELECT (SELECT TOP 1 u1.NombreUsuario FROM CREDITO.Credito c1 
					INNER JOIN MAESTRO.Usuario U1 ON U1.UsuarioId = c1.UsuarioRegId
					--INNER JOIN MAESTRO.Persona p1 ON p1.PersonaId = U1.PersonaId
					WHERE c1.PersonaId=c.ClienteId 
					ORDER BY c1.CreditoId DESC) 'Agente',
			p.NumeroDocumento,p.NombreCompleto 'Cliente', 
			p.Direccion,p.DireccionRef,p.Celular1 'Celular',c.Calificacion,
			c.Nota
			-- c.DireccionNegocio,c.DireccionNegocioRef
	FROM MAESTRO.Cliente C
	INNER JOIN  ClientesBase B ON C.PersonaId = B.PersonaId
	INNER JOIN MAESTRO.Persona P ON P.PersonaId = C.PersonaId
	WHERE C.Bloqueado=1
	ORDER BY Agente,Cliente
