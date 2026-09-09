
-- CREDITO.usp_RptCajaDiario @UsuarioId=1026

CREATE PROC	[CREDITO].[usp_RptCajaDiario]
@UsuarioId INT = NULL,
@OficinaId INT = NULL,
@FechaInicio Date = NULL,
@FechaFin Date = NULL
AS

IF	@FechaInicio IS NULL
BEGIN
	SET @FechaInicio = '20000101'
    SET @FechaFin = GETDATE()
END
	

SELECT cd.CajaDiarioId,o.Denominacion 'Oficina', c.Denominacion 'Caja',p.NombreCompleto 'Agente',
		cd.SaldoInicial,cd.Entradas,cd.Salidas,cd.SaldoFinal,cd.FechaIniOperacion,cd.FechaFinOperacion
		--cd.MontoPorCobrar,cd.MontoCobrado
FROM CREDITO.CajaDiario cd
INNER JOIN CREDITO.Caja c ON c.CajaId = cd.CajaId
INNER JOIN MAESTRO.Oficina o ON o.OficinaId = c.OficinaId
INNER JOIN MAESTRO.Usuario u ON u.UsuarioId = cd.UsuarioAsignadoId
INNER JOIN MAESTRO.Persona p ON p.PersonaId = u.PersonaId
WHERE cd.IndCierre=1 AND cd.TransBoveda=1
AND c.OficinaId = ISNULL(@OficinaId,c.OficinaId)  
AND cd.UsuarioAsignadoId = ISNULL(@UsuarioId,cd.UsuarioAsignadoId) 
AND CAST(cd.FechaIniOperacion AS DATE) BETWEEN @FechaInicio AND @FechaFin
