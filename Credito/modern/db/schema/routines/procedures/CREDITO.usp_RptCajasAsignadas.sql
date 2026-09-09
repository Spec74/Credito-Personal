
CREATE PROCEDURE [CREDITO].[usp_RptCajasAsignadas]
   @OficinaId INT=1
AS


SELECT CD.CajaDiarioId, C.Denominacion 'Caja', IIF(CD.IndCierre=0,'ABIERTO','CERRADO') 'Modo', P.NombreCompleto 'Cajero',
		CD.FechaIniOperacion,CD.FechaFinOperacion,CD.SaldoInicial,CD.Salidas, CD.Entradas, CD.SaldoFinal, 
		dbo.ufnResumenCuentaCajaDiario(CD.CajaDiarioId) 'Resumen'
FROM CREDITO.CajaDiario CD
INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
INNER JOIN MAESTRO.Usuario U ON U.UsuarioId = CD.UsuarioAsignadoId
INNER JOIN MAESTRO.Persona P ON P.PersonaId = U.PersonaId
WHERE CD.TransBoveda = 0 AND C.OficinaId=@OficinaId
ORDER BY 1
