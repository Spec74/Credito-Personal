-- exec CREDITO.usp_RptCobroDiario 8

CREATE PROC [CREDITO].[usp_RptCobroDiario]
    @UsuarioId INT = NULL,
    @OficinaId INT = NULL 
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @FechaAct DATE = dbo.ufnFecha();

    ;WITH PlanPagoAgregado AS (
        SELECT 
            CreditoId,
            MIN(CASE WHEN Estado = 'PEN' THEN FechaVencimiento END) AS MinFechaVencimiento,
            COUNT(CASE WHEN Estado = 'PEN' AND FechaVencimiento < @FechaAct THEN 1 END) AS NroCuotasPen,
            SUM(CASE WHEN Estado = 'PEN' AND FechaVencimiento < @FechaAct THEN Cuota + Cargo - PagoLibre ELSE 0 END) AS CuotaAcumulada,
            SUM(CASE WHEN Estado = 'PEN' THEN Cuota ELSE 0 END) AS SumaCuota,
            SUM(Cuota + Cargo) AS MontoTotal,
			MIN(Cuota) AS CuotaPlan
        FROM CREDITO.PlanPago
        GROUP BY CreditoId
    ),
    CuotaPendienteRanked AS (
        SELECT 
            CreditoId,
            Cuota + Cargo - PagoLibre AS CuotaPendiente,
            ROW_NUMBER() OVER (PARTITION BY CreditoId ORDER BY Numero) AS rn
        FROM CREDITO.PlanPago
        WHERE Estado = 'PEN'
    ),
    MovimientoCajaAgregado AS (
        SELECT 
            CreditoId,
            SUM(ImportePago) AS MovCaja
        FROM CREDITO.MovimientoCaja
        WHERE Operacion = 'CUO' AND Estado = 1
        GROUP BY CreditoId
    ),
    DESEMBOLSOS AS (
        SELECT 
            C.CreditoId,
            CAST(SUBSTRING(P.Codigo, 3, LEN(P.Codigo)) AS INT) AS Orden,
            dbo.ufnCalcularDiasAtrazo(PPA.MinFechaVencimiento, @FechaAct) AS DiasAtrazo,
            PPA.MinFechaVencimiento AS FechaPago,
            PPA.NroCuotasPen,
			PPA.CuotaPlan,
            NULLIF(PPA.CuotaAcumulada, 0) AS CuotaAcumulada,
            CPR.CuotaPendiente,
            PPA.SumaCuota,
            MCA.MovCaja,
            PPA.MontoTotal,
            dbo.ufnCalcularDiasAtrazo(C.FechaVencimiento, @FechaAct) AS DiasAtrazoMora
        FROM CREDITO.Credito C
        INNER JOIN MAESTRO.Persona P ON C.PersonaId = P.PersonaId
        LEFT JOIN PlanPagoAgregado PPA ON C.CreditoId = PPA.CreditoId
        LEFT JOIN CuotaPendienteRanked CPR ON C.CreditoId = CPR.CreditoId AND CPR.rn = 1
        LEFT JOIN MovimientoCajaAgregado MCA ON C.CreditoId = MCA.CreditoId
        WHERE C.Estado = 'DES' 
          AND (C.OficinaId = @OficinaId OR @OficinaId IS NULL)
          AND (C.UsuarioRegId = @UsuarioId OR @UsuarioId IS NULL)
    )
    SELECT 
        ROW_NUMBER() OVER (ORDER BY P.NombreCompleto) AS Nro,
        CM.Orden,
        C.CreditoId,
        P.NombreCompleto AS Cliente,
        P.Celular1 AS Celular,		
        C.MontoCredito,
        C.Interes,
		CM.CuotaPlan,
        CM.MontoTotal - ISNULL(CM.MovCaja, 0) AS Saldo,
        CM.DiasAtrazo,
        CM.NroCuotasPen,		
        ISNULL(CM.CuotaAcumulada, CM.CuotaPendiente) AS CuotaTotal,
        P.Direccion,
        CM.FechaPago,
        C.FechaPrimerPago,
        C.FechaVencimiento,
        CONVERT(DECIMAL(10,2), dbo.ufnCalcularMora(CM.SumaCuota, CM.DiasAtrazoMora, 0)) AS Mora,
        CM.MontoTotal,
        O.Denominacion AS Negocio,
        C.FormaPago,
		CL.TopeCredito,
		SBS.DesCorta 'ClasificacionRiesgoSBS'
    FROM CREDITO.Credito C
    INNER JOIN DESEMBOLSOS CM ON C.CreditoId = CM.CreditoId
    INNER JOIN MAESTRO.Persona P ON C.PersonaId = P.PersonaId
    INNER JOIN MAESTRO.Cliente CL ON C.PersonaId = CL.ClienteId
	LEFT JOIN MAESTRO.ValorTabla SBS ON SBS.TablaId=14 AND CL.ClasificacionRiesgoSBS = SBS.ItemId
    LEFT JOIN MAESTRO.Ocupacion O ON CL.ActividadEconId = O.OcupacionId;
END;
