
CREATE PROCEDURE [CREDITO].[usp_UsuariosNoAsignadosCaja]
@OficinaId INT
AS

;WITH USUARIO_CAJA AS(
	SELECT CD.UsuarioAsignadoId 
	FROM CREDITO.CajaDiario CD
	INNER JOIN CREDITO.Caja C ON CD.CajaId = C.CajaId
	WHERE	C.OficinaId=@OficinaId AND IndCierre=0 and TransBoveda=0
)
SELECT U.UsuarioId 'Id', P.NombreCompleto 'Valor'
FROM MAESTRO.Rol R
INNER JOIN MAESTRO.UsuarioRol UR ON R.RolId = UR.RolId
INNER JOIN MAESTRO.Usuario U ON UR.UsuarioId = U.UsuarioId
INNER JOIN MAESTRO.UsuarioOficina UO ON U.UsuarioId = UO.UsuarioId
INNER JOIN MAESTRO.Persona P ON U.PersonaId = P.PersonaId
LEFT JOIN USUARIO_CAJA UC ON UC.UsuarioAsignadoId = U.UsuarioId
WHERE	UO.OficinaId=@OficinaId AND U.Estado=1 AND P.Estado=1 
		AND R.Denominacion like 'CAJA' AND UC.UsuarioAsignadoId IS NULL
