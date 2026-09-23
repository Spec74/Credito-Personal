

CREATE PROC [CREDITO].[usp_RptClientesInactivos]
	@UsuarioId INT = NULL,
	@OficinaId INT = NULL,
	@FechaInicio Date = NULL,
	@FechaFin Date = NULL
AS
BEGIN
	SET NOCOUNT ON;

	IF @FechaInicio IS NULL
	BEGIN
		-- ==============================================================================
		-- BLOQUE 1: INACTIVOS HISTÓRICOS (El cliente pertenece a su ÚLTIMO agente absoluto)
		-- ==============================================================================
		;WITH UltimoCreditoCliente AS (
			SELECT C.PersonaId, C.UsuarioRegId, C.OficinaId, C.MontoCredito, C.FechaPagado, C.FechaMod,
			       ROW_NUMBER() OVER (PARTITION BY C.PersonaId ORDER BY C.CreditoId DESC) as Rn
			FROM CREDITO.Credito C
			WHERE C.Estado IN ('PAG', 'ANU')
		),
		ClientesBase AS (
			SELECT PersonaId, UsuarioRegId, MontoCredito, FechaPagado, FechaMod
			FROM UltimoCreditoCliente
			WHERE Rn = 1 
			  AND (@OficinaId IS NULL OR OficinaId = @OficinaId)
			  AND (@UsuarioId IS NULL OR UsuarioRegId = @UsuarioId)
		),
		ConteoCreditos AS (
			SELECT PersonaId, COUNT(1) AS TotalCreditos
			FROM CREDITO.Credito
			GROUP BY PersonaId
		),
		MaxAtrasoCliente AS (
			-- Buscamos el peor retraso histórico del cliente en la Central de Riesgo
			SELECT CB.PersonaId, MAX(ISNULL(CR.DiasAtrazo, 0)) AS MaxDiasAtrazo
			FROM ClientesBase CB
			LEFT JOIN CREDITO.Credito C ON CB.PersonaId = C.PersonaId
			LEFT JOIN CREDITO.CentralRiesgo CR ON C.CreditoId = CR.CreditoId
			GROUP BY CB.PersonaId
		)
		SELECT	cb.PersonaId, u.NombreUsuario AS 'Agente', P.Codigo, P.NumeroDocumento 'Dni', p.NombreCompleto 'Cliente', 
				p.Direccion, p.DireccionRef, p.Celular1 'Celular', c.Calificacion, c.DireccionNegocio, c.DireccionNegocioRef, 
				ISNULL(cb.MontoCredito, 0) AS 'MontoCredito', 
				
				-- NUEVO: Tope de Crédito del Cliente
				ISNULL(c.TopeCredito, 0) AS 'TopeCredito',
				
				-- 1. Fecha de Cancelación sin hora
				CAST(ISNULL(cb.FechaPagado, cb.FechaMod) AS DATE) AS 'FechaCancelacion',
				
				ISNULL(cc.TotalCreditos, 0) AS 'TotalCreditos', 
				DATEDIFF(DAY, ISNULL(cb.FechaPagado, cb.FechaMod), GETDATE()) AS 'DiasInactividad',
				
				-- 2. Indicador de Depuración
				CASE WHEN EXISTS (SELECT 1 FROM MAESTRO.PersonaDepurado pd WHERE pd.PersonaId = cb.PersonaId AND pd.Estado = 1) 
					 THEN 'SÍ' ELSE 'NO' END AS 'Depurado',
				
				-- 3. Clasificación de Riesgo SBS Histórica Matemática
				CASE 
					WHEN ISNULL(mac.MaxDiasAtrazo, 0) <= 8 THEN 'NORMAL'
					WHEN mac.MaxDiasAtrazo BETWEEN 9 AND 30 THEN 'CPP'
					WHEN mac.MaxDiasAtrazo BETWEEN 31 AND 60 THEN 'DEFICIENTE'
					WHEN mac.MaxDiasAtrazo BETWEEN 61 AND 120 THEN 'DUDOSO'
					ELSE 'PERDIDA'
				END AS 'ClasificacionRiesgoSBS'

		FROM MAESTRO.Cliente c
		INNER JOIN ClientesBase cb ON c.PersonaId = cb.PersonaId
		INNER JOIN ConteoCreditos cc ON c.PersonaId = cc.PersonaId
		INNER JOIN MaxAtrasoCliente mac ON cb.PersonaId = mac.PersonaId
		INNER JOIN MAESTRO.Persona p ON cb.PersonaId = p.PersonaId
		INNER JOIN MAESTRO.Usuario u ON cb.UsuarioRegId = u.UsuarioId 
		WHERE c.Estado = 1
		  AND NOT EXISTS (SELECT 1 FROM CREDITO.Credito c1 WHERE c1.PersonaId = cb.PersonaId AND c1.Estado = 'DES')
		-- NUEVO ORDEN: Primero los NO Depurados, luego por Agente y Cliente
		ORDER BY 
			CASE WHEN EXISTS (SELECT 1 FROM MAESTRO.PersonaDepurado pd WHERE pd.PersonaId = cb.PersonaId AND pd.Estado = 1) THEN 1 ELSE 0 END ASC,
			u.NombreUsuario, 
			p.NombreCompleto
	END
	ELSE
	BEGIN 
		-- ==============================================================================
		-- BLOQUE 2: INACTIVOS POR RANGO DE FECHAS
		-- ==============================================================================
		;WITH CreditosCerrados AS (
			SELECT C.PersonaId, C.UsuarioRegId, C.OficinaId, C.MontoCredito, C.FechaPagado, C.FechaMod,
			       ROW_NUMBER() OVER (PARTITION BY C.PersonaId ORDER BY C.FechaPagado DESC) as Rn
			FROM CREDITO.Credito C
			WHERE C.Estado IN ('PAG', 'ANU')
			  AND (@OficinaId IS NULL OR C.OficinaId = @OficinaId)  
			  AND (@UsuarioId IS NULL OR C.UsuarioRegId = @UsuarioId) 
			  AND CAST(ISNULL(C.FechaPagado, C.FechaMod) AS DATE) BETWEEN @FechaInicio AND @FechaFin
		),
		ClientesBase AS (
			SELECT PersonaId, UsuarioRegId, MontoCredito, FechaPagado, FechaMod FROM CreditosCerrados WHERE Rn = 1
		),
		ConteoCreditos AS (
			SELECT PersonaId, COUNT(1) AS TotalCreditos FROM CREDITO.Credito GROUP BY PersonaId
		),
		MaxAtrasoCliente AS (
			-- Buscamos el peor retraso histórico del cliente en la Central de Riesgo
			SELECT CB.PersonaId, MAX(ISNULL(CR.DiasAtrazo, 0)) AS MaxDiasAtrazo
			FROM ClientesBase CB
			LEFT JOIN CREDITO.Credito C ON CB.PersonaId = C.PersonaId
			LEFT JOIN CREDITO.CentralRiesgo CR ON C.CreditoId = CR.CreditoId
			GROUP BY CB.PersonaId
		)	
		SELECT	cb.PersonaId, u.NombreUsuario AS 'Agente', P.Codigo, P.NumeroDocumento 'Dni', p.NombreCompleto 'Cliente', 
				p.Direccion, p.DireccionRef, p.Celular1 'Celular', c.Calificacion, c.DireccionNegocio, c.DireccionNegocioRef, 
				ISNULL(cb.MontoCredito, 0) AS 'MontoCredito', 
				
				-- NUEVO: Tope de Crédito del Cliente
				ISNULL(c.TopeCredito, 0) AS 'TopeCredito',
				
				-- 1. Fecha de Cancelación sin hora
				CAST(ISNULL(cb.FechaPagado, cb.FechaMod) AS DATE) AS 'FechaCancelacion',
				
				ISNULL(cc.TotalCreditos, 0) AS 'TotalCreditos', 
				DATEDIFF(DAY, ISNULL(cb.FechaPagado, cb.FechaMod), GETDATE()) AS 'DiasInactividad',

				-- 2. Indicador de Depuración
				CASE WHEN EXISTS (SELECT 1 FROM MAESTRO.PersonaDepurado pd WHERE pd.PersonaId = cb.PersonaId AND pd.Estado = 1) 
					 THEN 'SÍ' ELSE 'NO' END AS 'Depurado',
				
				-- 3. Clasificación de Riesgo SBS Histórica Matemática
				CASE 
					WHEN ISNULL(mac.MaxDiasAtrazo, 0) <= 8 THEN 'NORMAL'
					WHEN mac.MaxDiasAtrazo BETWEEN 9 AND 30 THEN 'CPP'
					WHEN mac.MaxDiasAtrazo BETWEEN 31 AND 60 THEN 'DEFICIENTE'
					WHEN mac.MaxDiasAtrazo BETWEEN 61 AND 120 THEN 'DUDOSO'
					ELSE 'PERDIDA'
				END AS 'ClasificacionRiesgoSBS'

		FROM MAESTRO.Cliente c
		INNER JOIN ClientesBase cb ON c.PersonaId = cb.PersonaId
		INNER JOIN ConteoCreditos cc ON c.PersonaId = cc.PersonaId
		INNER JOIN MaxAtrasoCliente mac ON cb.PersonaId = mac.PersonaId
		INNER JOIN MAESTRO.Persona p ON cb.PersonaId = p.PersonaId
		INNER JOIN MAESTRO.Usuario u ON cb.UsuarioRegId = u.UsuarioId 
		WHERE c.Estado = 1
		  AND NOT EXISTS (SELECT 1 FROM CREDITO.Credito c1 WHERE c1.PersonaId = cb.PersonaId AND c1.Estado = 'DES')
		-- NUEVO ORDEN: Primero los NO Depurados, luego por Agente y Cliente
		ORDER BY 
			CASE WHEN EXISTS (SELECT 1 FROM MAESTRO.PersonaDepurado pd WHERE pd.PersonaId = cb.PersonaId AND pd.Estado = 1) THEN 1 ELSE 0 END ASC,
			u.NombreUsuario, 
			p.NombreCompleto
	END
END

