/*
   EXEC CREDITO.usp_ListarSaldoCartera NULL,NULL,1
*/
CREATE PROC [CREDITO].[usp_ListarSaldoCartera]
 @Anio INT=NULL ,
 @Mes INT=NULL,
 @OficinaId INT = NULL,
 @UsuarioId INT = NULL 
AS

DECLARE @FechaAct DATE=dbo.ufnFecha()
IF @Anio IS NULL
BEGIN	
	SET @Anio = YEAR(@FechaAct)
	SET @Mes = MONTH(@FechaAct)
END

DECLARE	@FechaInicio DATE = CAST(@Anio AS VARCHAR(4)) + '-' + CAST(@Mes AS VARCHAR(2))  + '-01' 
DECLARE	@FechaFin DATE = CAST(@Anio AS VARCHAR(4)) + '-' + CAST(@Mes AS VARCHAR(2))  + '-01' 
SET @FechaFin = DATEADD(DAY,-1,DATEADD(MONTH,1,@FechaFin)) ;

IF YEAR(@FechaAct) = @Anio AND MONTH(@FechaAct) = @Mes
BEGIN
	-- CALCULA SALDO CARTERA A LA FECHA ACTUAL
	;WITH DESEMBOLSOS AS(
		SELECT	C.CreditoId,C.UsuarioRegId,C.PersonaId,C.OficinaId,C.MontoDesembolso,
					ISNULL((SELECT SUM(ImportePago) FROM CREDITO.MovimientoCaja 
					WHERE CreditoId=C.CreditoId and ImportePago>0 and Operacion='CUO' and Estado=1),0) 'MovCaja',
					(SELECT SUM(Cuota + Cargo) FROM CREDITO.PlanPago 
					WHERE CreditoId=C.CreditoId ) 'MontoTotal',
					dbo.ufnCalcularDiasAtrazo(C.FechaVencimiento,@FechaAct) 'DiasAtrazoMora'
		FROM	CREDITO.Credito C
		INNER JOIN MAESTRO.Persona P ON C.PersonaId=P.PersonaId
		WHERE	C.Estado='DES' AND C.OficinaId=ISNULL(@OficinaId,C.OficinaId) 
		AND		C.UsuarioRegId=ISNULL(@UsuarioId,C.UsuarioRegId)
	),
	COBRODIARIO AS(
		SELECT	CM.CreditoId,CM.UsuarioRegId,CM.PersonaId,CM.OficinaId,CM.MontoDesembolso,
			( CM.MontoTotal - ISNULL(MovCaja,0) ) 'Saldo',
			CONVERT(decimal(10,2) , dbo.ufnCalcularMora(CM.MontoTotal - ISNULL(MovCaja,0), CM.DiasAtrazoMora,0)) 'Mora',
			CM.DiasAtrazoMora
		FROM DESEMBOLSOS CM
	),
	CLIENTE_NUEVOS AS (
		SELECT DISTINCT C.OficinaId, C.UsuarioRegId, C.PersonaId 
		FROM CREDITO.Credito C
		INNER JOIN MAESTRO.Cliente CL  ON CL.PersonaId = C.PersonaId
		WHERE CAST( CL.FechaRegistro AS DATE) BETWEEN @FechaInicio AND @FechaFin
		AND C.OficinaId=ISNULL(@OficinaId,C.OficinaId) 
	),
	NRO_CLIENTES_NUEVOS AS(
		SELECT UsuarioRegId,OficinaId,COUNT(1) 'NroClientesNuevos'
		FROM CLIENTE_NUEVOS
		GROUP BY UsuarioRegId,OficinaId
	),
	SALDOCARTERA AS(
		SELECT UsuarioRegId,OficinaId,SUM(Saldo) 'Saldo',COUNT(DISTINCT PersonaId) 'NroClientes',
				COUNT(1) 'NroDesembolsos',SUM(MontoDesembolso) 'MontoDesembolsos'
		FROM COBRODIARIO
		GROUP BY UsuarioRegId,OficinaId
	),
	SALDOCARTERA_MORA AS(
		SELECT UsuarioRegId,OficinaId,SUM(Saldo) 'Saldo',COUNT(DISTINCT PersonaId) 'NroClientes',
				SUM(IIF(DiasAtrazoMora<=90,Saldo,0)) 'SaldoVencido',
				SUM(IIF(DiasAtrazoMora>90,Saldo,0)) 'SaldoMorosidad'
		FROM COBRODIARIO
		WHERE Mora>0
		GROUP BY UsuarioRegId,OficinaId
	)
	SELECT SC.UsuarioRegId 'AgenteId',SC.OficinaId,SC.NroDesembolsos,SC.MontoDesembolsos,
			SC.Saldo 'SaldoCartera',SC.NroClientes 'NroClientesSaldoCartera',
			ISNULL(SCM.Saldo,0.0) 'SaldoMoraCartera',ISNULL(SCM.NroClientes,0) 'NroClientesSaldoMoraCartera',
			ISNULL(SCM.SaldoVencido,0) 'SaldoVencido',ISNULL(SCM.SaldoMorosidad,0) 'SaldoMorosidad', 
			ISNULL(N.NroClientesNuevos,0) 'NroClientesNuevos',
			dbo.ufnFecha() 'FechaCierre'
	FROM SALDOCARTERA SC
	LEFT JOIN SALDOCARTERA_MORA SCM ON SCM.UsuarioRegId = SC.UsuarioRegId AND SCM.OficinaId = SC.OficinaId
	LEFT JOIN NRO_CLIENTES_NUEVOS N ON N.UsuarioRegId=SC.UsuarioRegId AND N.OficinaId = SC.OficinaId
END
ELSE
BEGIN

	SELECT AgenteId,OficinaId,NroDesembolsos,MontoDesembolsos,
			SaldoCartera,NroClientesSaldoCartera,
			SaldoMoraCartera, NroClientesSaldoMoraCartera,
			SaldoVencido,SaldoMorosidad,NroClientesNuevos,FechaCierre 
	FROM CREDITO.SaldoCarteraMensual
	WHERE Anio=@anio AND Mes=@Mes
	AND OficinaId = ISNULL(@OficinaId,OficinaId)  
	AND AgenteId = ISNULL(@UsuarioId,AgenteId) 
	
	--;WITH AGENTES AS (
	--		SELECT DISTINCT cd.UsuarioAsignadoId 'AgenteId',c.OficinaId,cd.CajaId 
	--		FROM CREDITO.CajaDiario cd
	--			INNER JOIN CREDITO.Caja c ON c.CajaId = cd.CajaId
	--		WHERE cd.IndCierre=1 AND cd.TransBoveda=1
	--			AND c.OficinaId = ISNULL(@OficinaId,c.OficinaId)  
	--			AND cd.UsuarioAsignadoId = ISNULL(@UsuarioId,cd.UsuarioAsignadoId) 
	--			AND CAST(cd.FechaIniOperacion AS DATE) BETWEEN @FechaInicio AND @FechaFin 
	--	),
	--CAJADIARIOS AS (
	--	SELECT A.AgenteId, A.OficinaId,A.CajaId ,
	--			 (SELECT TOP 1 CajaDiarioId FROM Credito.CajaDiario 
	--			 WHERE UsuarioAsignadoId=A.AgenteId 
	--			 AND CAST(FechaIniOperacion AS DATE) BETWEEN DATEADD(MONTH,-1,DATEADD(DAY,1,@FechaFin)) AND @FechaFin
	--			 ORDER BY FechaIniOperacion DESC) 'CajaDiarioIdFin'				 
	--	FROM AGENTES A
	--)
	--SELECT c.AgenteId, C.OficinaId, 0 'NroDesembolsos',
	--		cdf.SaldoCartera,cdf.NroClientesSaldoCartera,
	--		cdf.SaldoMoraCartera,cdf.NroClientesSaldoMoraCartera,
	--		0 'SaldoVencido',0 'SaldoMorosidad'
	--FROM CAJADIARIOS C	
	--LEFT JOIN CREDITO.CajaDiario cdf ON cdf.CajaDiarioId = C.CajaDiarioIdFin
END
