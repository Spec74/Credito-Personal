
 -- CREDITO.usp_ActualizarSaldosCajaDiario 10,1
 
 CREATE PROC [CREDITO].[usp_ActualizarSaldosCajaDiario]
 @UsuarioId INT = NULL ,
 @OficinaId INT = NULL 
 AS

	DECLARE @tRptSaldos TABLE(AgenteId INT,OficinaId INT, NroDesembolsos INT, SaldoCartera DECIMAL(16,2), NroClientesSaldoCartera INT,
									SaldoMoraCartera DECIMAL(16,2),NroClientesSaldoMoraCartera INT,SaldoVencido DECIMAL(16,2),SaldoMorosidad DECIMAL(16,2))

	INSERT INTO @tRptSaldos  
	EXEC CREDITO.usp_ListarSaldoCartera NULL,NULL, @OficinaId, @UsuarioId


UPDATE cd
SET SaldoCartera = ISNULL(s.SaldoCartera,0) , NroClientesSaldoCartera=s.NroClientesSaldoCartera,
	SaldoMoraCartera=	ISNULL(s.SaldoMoraCartera,0), NroClientesSaldoMoraCartera=s.NroClientesSaldoMoraCartera
--SELECT * 
FROM CREDITO.CajaDiario cd
INNER JOIN @tRptSaldos s ON cd.UsuarioAsignadoId=s.AgenteId
WHERE cd.UsuarioAsignadoId = @UsuarioId AND cd.IndCierre=0

--DECLARE @FechaAct DATE=dbo.ufnFecha()
--DECLARE @tRptCobroDiario TABLE(Saldo DECIMAL(16,2),Mora DECIMAL(16,2))

--;WITH DESEMBOLSOS AS(
--	SELECT	C.CreditoId,
--				(SELECT SUM(ImportePago) FROM CREDITO.MovimientoCaja 
--				WHERE CreditoId=C.CreditoId and ImportePago>0 and Operacion='CUO' and Estado=1) 'MovCaja',
--				(SELECT SUM(Cuota + Cargo) FROM CREDITO.PlanPago 
--				WHERE CreditoId=C.CreditoId ) 'MontoTotal',
--				dbo.ufnCalcularDiasAtrazo(C.FechaVencimiento,@FechaAct) 'DiasAtrazoMora'						
--	FROM	CREDITO.Credito C
--	INNER JOIN MAESTRO.Persona P ON C.PersonaId=P.PersonaId
--	WHERE	C.Estado='DES' AND C.OficinaId=ISNULL(@OficinaId,C.OficinaId) 
--	AND		C.UsuarioRegId=ISNULL(@UsuarioId,C.UsuarioRegId) 
--)
--INSERT INTO @tRptCobroDiario
--SELECT	( CM.MontoTotal - ISNULL(MovCaja,0) ) 'Saldo',
--		CONVERT(decimal(10,2) , dbo.ufnCalcularMora(CM.MontoTotal - ISNULL(MovCaja,0), CM.DiasAtrazoMora,0)) 'Mora'
--FROM CREDITO.Credito C
--INNER JOIN DESEMBOLSOS CM ON C.CreditoId = CM.CreditoId


--DECLARE @SaldoCartera DECIMAL(16,2),@SaldoMoraCartera DECIMAL(16,2)
--DECLARE @NroClientesSaldoCartera INT,@NroClientesSaldoMoraCartera INT

--SELECT @SaldoCartera =SUM(Saldo), @NroClientesSaldoCartera = COUNT(1)
--FROM @tRptCobroDiario

--SELECT @SaldoMoraCartera = SUM(Saldo), @NroClientesSaldoMoraCartera = COUNT(1)
--FROM @tRptCobroDiario
--WHERE Mora>0

--SELECT @SaldoCartera,@NroClientesSaldoCartera,@SaldoMoraCartera,@NroClientesSaldoMoraCartera
    
--UPDATE CREDITO.CajaDiario 
--SET SaldoCartera = ISNULL(@SaldoCartera,0) , NroClientesSaldoCartera=@NroClientesSaldoCartera,
--	SaldoMoraCartera=	ISNULL(@SaldoMoraCartera,0), NroClientesSaldoMoraCartera=@NroClientesSaldoMoraCartera
--WHERE UsuarioAsignadoId=@UsuarioId AND IndCierre=0
--GO
