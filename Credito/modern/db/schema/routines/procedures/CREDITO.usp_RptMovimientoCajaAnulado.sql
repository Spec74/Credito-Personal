/*
EXEC [CREDITO].[usp_RptMovimientoCajaAnulado] '20220201','20220301'
*/
CREATE PROC [CREDITO].[usp_RptMovimientoCajaAnulado]
@FechaIni DATE ,
@FechaFin DATE 
AS

SELECT MC.MovimientoCajaId,MC.Operacion, MC.ImportePago, p.NombreCompleto 'Persona', MC.Descripcion, 
		MC.FechaReg, u.NombreUsuario 'UsuarioRegistro',
		MCA.Observacion 'MotivoAnulacion', MCA.FechaReg 'FechaAnulacion', UA.NombreUsuario 'UsuarioAnulacion'

FROM CREDITO.MovimientoCaja MC
INNER JOIN CREDITO.MovimientoCajaAnu MCA ON MCA.MovimientoCajaId = MC.MovimientoCajaId 
INNER JOIN MAESTRO.Persona P ON P.PersonaId = MC.PersonaId
INNER JOIN MAESTRO.Usuario U ON U.UsuarioId = MC.UsuarioRegId
INNER JOIN MAESTRO.Usuario UA ON UA.UsuarioId = MCA.UsuarioRegId
WHERE MC.Estado = 0
AND MC.FechaReg BETWEEN @FechaIni AND @FechaFin
