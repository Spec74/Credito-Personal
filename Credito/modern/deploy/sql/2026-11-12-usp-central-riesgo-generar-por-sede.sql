-- Central de riesgos por sede: cache/lectura (Anio, Mes, OficinaId).
-- Idempotente. Fuente canónica: db/schema/routines/procedures/CREDITO.usp_CentralRiesgoGenerar.sql
-- Tras aplicar: generar TXT por sede; si el mes quedó congelado con otra sede, use @Regenerar=1.

SET NOCOUNT ON;
GO


/*

exec Credito.usp_CentralRiesgoGenerar 1,2016,8

Cache y lectura por sede: (Anio, Mes, OficinaId).
No congela el mes de toda la red con la primera sede que genera.

*/
CREATE OR ALTER PROC [CREDITO].[usp_CentralRiesgoGenerar]
@OficinaId INT,
@Anio INT,
@Mes INT,
@Regenerar BIT = 0
AS
BEGIN
	SET NOCOUNT ON;

	IF @OficinaId IS NULL OR @OficinaId < 1
	BEGIN
		RAISERROR(N'OficinaId es obligatorio y debe ser >= 1 (central de riesgos por sede).', 16, 1);
		RETURN;
	END

	IF @Anio < 1900 OR @Anio > 2100 OR @Mes < 1 OR @Mes > 12
	BEGIN
		RAISERROR(N'Anio/Mes inválidos.', 16, 1);
		RETURN;
	END

	IF @Regenerar = 1
	BEGIN
		DELETE CR
		FROM CREDITO.CentralRiesgo CR
		INNER JOIN CREDITO.Credito C ON CR.CreditoId = C.CreditoId
		WHERE CR.Anio = @Anio
		  AND CR.Mes = @Mes
		  AND C.OficinaId = @OficinaId;
	END

	IF NOT EXISTS (
		SELECT 1
		FROM CREDITO.CentralRiesgo CR
		INNER JOIN CREDITO.Credito C ON CR.CreditoId = C.CreditoId
		WHERE CR.Anio = @Anio
		  AND CR.Mes = @Mes
		  AND C.OficinaId = @OficinaId
	)
	BEGIN
		DECLARE @FechaAct DATE = DATEADD(DAY, -1, DATEADD(MONTH, 1, DATEFROMPARTS(@Anio, @Mes, 1)));
		DECLARE @tblCreditoMora TABLE(
			id INT IDENTITY(1,1),
			CreditoId INT,
			CuotasAtrazo INT,
			CapitalAtrazo DECIMAL(16,2),
			GA DECIMAL(16,2),
			InteresAtrazo DECIMAL(16,2),
			Mora DECIMAL(16,2),
			ImporteLibre DECIMAL(16,2),
			DiasAtrazo INT,
			DeudaAtrazo DECIMAL(16,2)
		);
		DECLARE @tCuotasPendientes TABLE(
			Id INT IDENTITY(1,1),
			PlanPagoId INT,
			Glosa VARCHAR(MAX),
			FechaVencimiento DATE,
			Amortizacion DECIMAL(16,2),
			Interes DECIMAL(16,2),
			GastosAdm DECIMAL(16,2),
			Cuota DECIMAL(16,2),
			DiasAtrazo INT,
			ImporteMora DECIMAL(16,2),
			Descuento DECIMAL(16,2),
			Cargo DECIMAL(16,2),
			PagoLibre DECIMAL(16,2),
			PagoCuota DECIMAL(16,2)
		);

		;WITH DESEMBOLSOS AS (
			SELECT	C.CreditoId,
					dbo.ufnCalcularDiasAtrazo(
						(SELECT MIN(FechaVencimiento) FROM CREDITO.PlanPago WHERE CreditoId = C.CreditoId AND Estado = 'PEN'),
						@FechaAct
					) AS DiasAtrazo
			FROM	CREDITO.Credito C
			WHERE	C.Estado = 'DES'
			  AND	C.OficinaId = @OficinaId
			  AND	CAST(C.FechaDesembolso AS DATE) <= @FechaAct
		)
		INSERT INTO @tblCreditoMora (CreditoId, DiasAtrazo)
		SELECT	C.CreditoId, CM.DiasAtrazo
		FROM CREDITO.Credito C
		INNER JOIN DESEMBOLSOS CM ON C.CreditoId = CM.CreditoId
		WHERE CM.DiasAtrazo >= 1;

		DECLARE @index INT = 1,
				@Filas INT = (SELECT COUNT(1) FROM @tblCreditoMora),
				@CreditoId INT;

		WHILE (@index <= @Filas)
		BEGIN
			SELECT @CreditoId = CreditoId FROM @tblCreditoMora WHERE id = @index;

			DELETE FROM @tCuotasPendientes;
			INSERT INTO @tCuotasPendientes
			EXEC CREDITO.usp_CuotasPendientes @CreditoId, @FechaAct;

			UPDATE @tblCreditoMora
			SET CapitalAtrazo = (SELECT SUM(Amortizacion) FROM @tCuotasPendientes WHERE DiasAtrazo > 0),
				GA = (SELECT SUM(GastosAdm) FROM @tCuotasPendientes WHERE DiasAtrazo > 0),
				InteresAtrazo = (SELECT SUM(Interes) FROM @tCuotasPendientes WHERE DiasAtrazo > 0),
				Mora = (SELECT SUM(ImporteMora) FROM @tCuotasPendientes WHERE DiasAtrazo > 0),
				ImporteLibre = (SELECT SUM(PagoLibre) FROM @tCuotasPendientes WHERE DiasAtrazo > 0),
				DiasAtrazo = (SELECT MAX(DiasAtrazo) FROM @tCuotasPendientes WHERE DiasAtrazo > 0),
				CuotasAtrazo = (SELECT COUNT(1) FROM @tCuotasPendientes WHERE DiasAtrazo > 0),
				DeudaAtrazo = (SELECT SUM(PagoCuota) FROM @tCuotasPendientes WHERE DiasAtrazo > 0)
			WHERE CreditoId = @CreditoId;

			SET @index = @index + 1;
		END

		INSERT INTO CREDITO.CentralRiesgo (Anio, Mes, CreditoId, DiasAtrazo, CuotasAtrazo, DeudaAtrazo)
		SELECT	@Anio,
				@Mes,
				C.CreditoId,
				CR.DiasAtrazo,
				CR.CuotasAtrazo,
				CR.DeudaAtrazo
		FROM CREDITO.Credito C
		INNER JOIN @tblCreditoMora CR ON C.CreditoId = CR.CreditoId
		WHERE C.OficinaId = @OficinaId
		  AND NOT EXISTS (
				SELECT 1
				FROM CREDITO.CentralRiesgo X
				WHERE X.Anio = @Anio
				  AND X.Mes = @Mes
				  AND X.CreditoId = C.CreditoId
		  );
	END

	SELECT	@Anio AS Anio,
			@Mes AS Mes,
			C.CreditoId,
			CAST(@Anio AS varchar(4)) + RIGHT('00' + CAST(@Mes AS VARCHAR(2)), 2) AS Periodo,
			'007898' AS Entidad,
			CASE p.TipoDocumento WHEN 'DNI' THEN 1 WHEN 'RUC' THEN 6 ELSE 0 END AS TipoDoc,
			p.NumeroDocumento AS NumDoc,
			CASE p.TipoPersona WHEN 'J' THEN p.NombreCompleto ELSE '' END AS RazonSocial,
			CASE p.TipoPersona WHEN 'N' THEN p.ApePaterno ELSE '' END AS ApePat,
			CASE p.TipoPersona WHEN 'N' THEN p.ApeMaterno ELSE '' END AS ApeMat,
			CASE p.TipoPersona WHEN 'N' THEN p.Nombre ELSE '' END AS Nombres,
			CASE p.TipoPersona WHEN 'N' THEN 1 WHEN 'J' THEN 2 ELSE 0 END AS TipoPersona,
			5 AS ModalidadCredito,
			CASE WHEN CR.DiasAtrazo <= 30 THEN CAST(CR.DeudaAtrazo AS VARCHAR(13)) ELSE '' END AS DeudaMenor30,
			CASE WHEN CR.DiasAtrazo > 30 THEN CAST(CR.DeudaAtrazo AS VARCHAR(13)) ELSE '' END AS DeudaMayor30,
			CASE WHEN CR.DiasAtrazo > 0 AND CR.DiasAtrazo <= 8 THEN 0
				 WHEN CR.DiasAtrazo > 8 AND CR.DiasAtrazo <= 30 THEN 1
				 WHEN CR.DiasAtrazo > 30 AND CR.DiasAtrazo <= 60 THEN 2
				 WHEN CR.DiasAtrazo > 60 AND CR.DiasAtrazo <= 120 THEN 3
				 WHEN CR.DiasAtrazo > 120 THEN 4
				 ELSE 0
			END AS Calificacion,
			CR.DiasAtrazo,
			p.Direccion,
			p.Celular1 AS celular
	FROM CREDITO.CentralRiesgo CR
	INNER JOIN CREDITO.Credito C ON CR.CreditoId = C.CreditoId
	INNER JOIN MAESTRO.Persona p ON C.PersonaId = p.PersonaId
	WHERE CR.Anio = @Anio
	  AND CR.Mes = @Mes
	  AND C.OficinaId = @OficinaId;
END

--[1-8 días]	0-NORMAL
--[9-30 días]	1-CPP
--[31-60 días]	2-DEFICIENTE
--[61-120 días]	3-DUDOSO
--[121 días a más]	4-PERDIDA

