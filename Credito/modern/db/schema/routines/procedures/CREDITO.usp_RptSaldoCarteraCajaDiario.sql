
/*
CREDITO.usp_RptSaldoCarteraCajaDiario @UsuarioId=null,@AnioIni=2023,@MesIni=6,@AnioFin=2023,@MesFin=7
*/
CREATE PROC	[CREDITO].[usp_RptSaldoCarteraCajaDiario]
@UsuarioId INT = NULL,
@OficinaId INT = NULL,
@AnioIni INT = NULL,
@MesIni INT = NULL,
@AnioFin INT = NULL,
@MesFin INT = NULL

AS
DECLARE	@FechaInicio DATE = CAST(@AnioIni AS VARCHAR(4)) + '-' + CAST(@MesIni AS VARCHAR(2))  + '-01' 
DECLARE	@FechaFin DATE = CAST(@AnioFin AS VARCHAR(4)) + '-' + CAST(@MesFin AS VARCHAR(2))  + '-01' 
SET @FechaFin = DATEADD(DAY,-1,DATEADD(MONTH,1,@FechaFin)) ;

DECLARE @tSaldoCarteraIni TABLE(AgenteId INT,OficinaId INT,NroDesembolsos INT,MontoDesembolsos DECIMAL(16,2),SaldoCartera DECIMAL(16,2),NroClientesSaldoCartera INT,
						SaldoMoraCartera DECIMAL(16,2),NroClientesSaldoMoraCartera INT,SaldoVencido DECIMAL(16,2),SaldoMorosidad DECIMAL(16,2),
						NroClientesNuevos INT, FechaCierre DATETIME)
DECLARE @tSaldoCarteraFin TABLE(AgenteId INT,OficinaId INT,NroDesembolsos INT,MontoDesembolsos DECIMAL(16,2),SaldoCartera DECIMAL(16,2),NroClientesSaldoCartera INT,
						SaldoMoraCartera DECIMAL(16,2),NroClientesSaldoMoraCartera INT,SaldoVencido DECIMAL(16,2),SaldoMorosidad DECIMAL(16,2),
						NroClientesNuevos INT, FechaCierre DATETIME)

INSERT INTO @tSaldoCarteraIni  
EXEC CREDITO.usp_ListarSaldoCartera @AnioIni,@MesIni,@OficinaId,@UsuarioId

INSERT INTO @tSaldoCarteraFin  
EXEC CREDITO.usp_ListarSaldoCartera @AnioFin,@MesFin,@OficinaId,@UsuarioId

;WITH AGENTES AS (
		SELECT DISTINCT cd.UsuarioAsignadoId 'AgenteId',c.OficinaId,cd.CajaId 
		FROM CREDITO.CajaDiario cd
			INNER JOIN CREDITO.Caja c ON c.CajaId = cd.CajaId
		WHERE cd.IndCierre=1 AND cd.TransBoveda=1
			AND c.OficinaId = ISNULL(@OficinaId,c.OficinaId)  
			AND cd.UsuarioAsignadoId = ISNULL(@UsuarioId,cd.UsuarioAsignadoId) 
			AND CAST(cd.FechaIniOperacion AS DATE) BETWEEN @FechaInicio AND @FechaFin 
	),
CAJADIARIOS AS (
	SELECT A.AgenteId, A.OficinaId,A.CajaId ,
			--(SELECT TOP 1 CajaDiarioId FROM Credito.CajaDiario 
			-- WHERE UsuarioAsignadoId=A.AgenteId 
			-- AND CAST(FechaIniOperacion AS DATE) BETWEEN @FechaInicio AND DATEADD(DAY,-1,DATEADD(MONTH,1,@FechaInicio))
			-- ORDER BY FechaIniOperacion DESC) 'CajaDiarioIdInicio',
			 --(SELECT SUM(Salidas) FROM Credito.CajaDiario 
			 --WHERE UsuarioAsignadoId=A.AgenteId 
			 --AND CAST(FechaIniOperacion AS DATE) BETWEEN @FechaInicio AND DATEADD(DAY,-1,DATEADD(MONTH,1,@FechaInicio))) 'SalidasIni',
			 (SELECT SUM(MontoPorCobrar) FROM Credito.CajaDiario 
			 WHERE UsuarioAsignadoId=A.AgenteId 
			 AND CAST(FechaIniOperacion AS DATE) BETWEEN @FechaInicio AND DATEADD(DAY,-1,DATEADD(MONTH,1,@FechaInicio))) 'MontoPorCobrarIni',
			 dbo.ufnCalcularImporteCobrado(@AnioIni,@MesIni,A.AgenteId) 'MontoCobradoIni',
			 --(SELECT TOP 1 CajaDiarioId FROM Credito.CajaDiario 
			 --WHERE UsuarioAsignadoId=A.AgenteId 
			 --AND CAST(FechaIniOperacion AS DATE) BETWEEN DATEADD(MONTH,-1,DATEADD(DAY,1,@FechaFin)) AND @FechaFin
			 --ORDER BY FechaIniOperacion DESC) 'CajaDiarioIdFin',
			 --(SELECT SUM(Salidas) FROM Credito.CajaDiario 
			 --WHERE UsuarioAsignadoId=A.AgenteId 
			 --AND CAST(FechaIniOperacion AS DATE) BETWEEN DATEADD(MONTH,-1,DATEADD(DAY,1,@FechaFin)) AND @FechaFin) 'SalidasFin',
			 (SELECT SUM(MontoPorCobrar) FROM Credito.CajaDiario 
			 WHERE UsuarioAsignadoId=A.AgenteId 
			 AND CAST(FechaIniOperacion AS DATE) BETWEEN DATEADD(MONTH,-1,DATEADD(DAY,1,@FechaFin)) AND @FechaFin) 'MontoPorCobrarFin',
			 dbo.ufnCalcularImporteCobrado(@AnioFin,@MesFin,A.AgenteId) 'MontoCobradoFin'
	FROM AGENTES A
)
SELECT c.AgenteId, o.Denominacion 'Oficina', CA.Denominacion 'Caja',p.NombreCompleto 'Agente',
		sci.FechaCierre  'FechaCierreIni',		
		ISNULL(sci.MontoDesembolsos,0) 'SalidasIni', 
		C.MontoCobradoIni,
		CAST(ROUND( IIF(C.MontoPorCobrarIni>0, (C.MontoCobradoIni*100)/C.MontoPorCobrarIni, 0)  ,2) AS DECIMAL(15,2)) 'PocentajeCobroIni',
		ISNULL((sci.SaldoCartera - sci.SaldoMoraCartera),0) 'SaldoCarteraSinMoraIni',
		ISNULL((sci.NroClientesSaldoCartera-sci.NroClientesSaldoMoraCartera),0) 'NroClientesCarteraSinMoraIni',
		ISNULL(sci.SaldoMoraCartera,0) 'SaldoMoraCarteraIni',ISNULL(sci.NroClientesSaldoMoraCartera,0) 'NroClientesSaldoMoraCarteraIni',
		ISNULL(sci.NroClientesNuevos,0) 'NroClientesNuevosIni',
		ISNULL(sci.SaldoVencido,0) 'SaldoVencidoIni',ISNULL(sci.SaldoMorosidad,0) 'SaldoMorosidadIni',
		scf.FechaCierre 'FechaCierreFin', 		
		ISNULL(scf.MontoDesembolsos,0) 'SalidasFin', 
		ISNULL(C.MontoCobradoFin,0) 'MontoCobradoFin',
		CAST(ROUND( IIF(C.MontoPorCobrarFin>0, (C.MontoCobradoFin*100)/C.MontoPorCobrarFin, 0)  ,2) AS DECIMAL(15,2)) 'PocentajeCobroFin',
		ISNULL((scf.SaldoCartera - scf.SaldoMoraCartera),0) 'SaldoCarteraSinMoraFin',
		ISNULL((scf.NroClientesSaldoCartera-scf.NroClientesSaldoMoraCartera),0) 'NroClientesCarteraSinMoraFin',
		ISNULL(scf.SaldoMoraCartera,0) 'SaldoMoraCarteraFin',ISNULL(scf.NroClientesSaldoMoraCartera,0) 'NroClientesSaldoMoraCarteraFin',
		ISNULL(scf.NroClientesNuevos,0) 'NroClientesNuevosFin',
		ISNULL(scf.SaldoVencido,0) 'SaldoVencidoFin',ISNULL(scf.SaldoMorosidad,0) 'SaldoMorosidadFin'
FROM CAJADIARIOS C
INNER JOIN CREDITO.Caja CA ON CA.CajaId = C.CajaId
INNER JOIN MAESTRO.Oficina o ON o.OficinaId = C.OficinaId
INNER JOIN MAESTRO.Usuario u ON u.UsuarioId = C.AgenteId
INNER JOIN MAESTRO.Persona p ON p.PersonaId = u.PersonaId
--LEFT JOIN CREDITO.CajaDiario cdi ON cdi.CajaDiarioId = C.CajaDiarioIdInicio
--LEFT JOIN CREDITO.CajaDiario cdf ON cdf.CajaDiarioId = C.CajaDiarioIdFin
LEFT JOIN @tSaldoCarteraIni sci ON sci.AgenteId = C.AgenteId
LEFT JOIN @tSaldoCarteraFin scf ON scf.AgenteId = C.AgenteId
ORDER BY p.NombreCompleto


--SELECT * FROM AGENTES
