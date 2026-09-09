
 -- CREDITO.usp_ActualizarSaldoCartera 1
 
 CREATE PROC [CREDITO].[usp_ActualizarSaldoCartera]
 @OficinaId INT 
 AS

	DECLARE @tRptSaldos TABLE(AgenteId INT,OficinaId INT, NroDesembolsos INT,MontoDesembolsos DECIMAL(16,2), SaldoCartera DECIMAL(16,2), NroClientesSaldoCartera INT,
									SaldoMoraCartera DECIMAL(16,2),NroClientesSaldoMoraCartera INT,SaldoVencido DECIMAL(16,2),SaldoMorosidad DECIMAL(16,2),
									NroClientesNuevos INT,FechaCierre DATETIME)

	INSERT INTO @tRptSaldos  
	EXEC CREDITO.usp_ListarSaldoCartera NULL,NULL, @OficinaId

	DECLARE @FechaAct DATE=dbo.ufnFecha(), @Anio INT , @Mes INT
	SET @Anio = YEAR(@FechaAct)
	SET @Mes = MONTH(@FechaAct)

	DELETE	FROM CREDITO.SaldoCarteraMensual 
	WHERE Anio=@Anio AND Mes=@Mes AND OficinaId=@OficinaId


	INSERT INTO CREDITO.SaldoCarteraMensual
			(OficinaId,AgenteId, Anio, Mes,
			NroDesembolsos,MontoDesembolsos,SaldoCartera,NroClientesSaldoCartera,
			SaldoMoraCartera,NroClientesSaldoMoraCartera,
			SaldoVencido,SaldoMorosidad,NroClientesNuevos, FechaCierre)
	SELECT OficinaId,AgenteId, @Anio 'Anio', @Mes 'Mes',
			NroDesembolsos,MontoDesembolsos,SaldoCartera,NroClientesSaldoCartera,
			SaldoMoraCartera,NroClientesSaldoMoraCartera,
			SaldoVencido,SaldoMorosidad,NroClientesNuevos, FechaCierre    
	FROM @tRptSaldos

--SELECT * FROM CREDITO.SaldoCartera
