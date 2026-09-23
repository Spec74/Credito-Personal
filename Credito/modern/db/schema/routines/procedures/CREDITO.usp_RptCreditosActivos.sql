--exec CREDITO.usp_RptCreditosActivos '20250901','20250930',8

CREATE PROC [CREDITO].[usp_RptCreditosActivos]
@FechaIni  DATE,
@FechaFin  DATE,
@UsuarioId INT = NULL,
@OficinaId INT = NULL
AS

DECLARE @FechaAct DATE = dbo.ufnFecha();

;WITH AGG_PP AS (
    SELECT
        CreditoId,
        MIN(CASE WHEN Estado = 'PEN' THEN FechaVencimiento END)                AS MinFechaVenPen,
        COUNT(CASE WHEN Estado = 'PEN' THEN 1 END)                             AS NroCuotasPen,
        SUM(CASE WHEN Estado = 'PEN' THEN Cuota - PagoLibre  ELSE 0 END)      AS Saldo,
        SUM(CASE WHEN Estado = 'PEN' THEN Amortizacion       ELSE 0 END)      AS SaldoCapital,
        ABS(SUM(CASE WHEN Estado = 'PEN' THEN Interes - PagoLibre ELSE 0 END))AS SaldoInteres,
        SUM(ISNULL(PagoCuota, 0) + PagoLibre)                                 AS Pagado,
        SUM(CASE WHEN Estado = 'PAG' THEN ISNULL(Interes, 0) ELSE 0 END)      AS InteresPagado,
        SUM(Interes)                                                            AS MontoInteres,
        SUM(Cuota)                                                              AS MontoCreditoTotal,
        SUM(ImporteMora)                                                        AS TotalImporteMora,
        MAX(CASE WHEN Numero = 1 THEN Cuota END)                               AS CuotaNro1
    FROM CREDITO.PlanPago
    GROUP BY CreditoId
),
DESEMBOLSOS AS (
    -- Creditos DESEMBOLSADOS activos
    --SELECT
    --    C.CreditoId,
    --    dbo.ufnCalcularDiasAtrazo(AP.MinFechaVenPen, @FechaAct)               AS DiasAtrazo,
    --    AP.NroCuotasPen,
    --    AP.Saldo,
    --    AP.SaldoCapital,
    --    AP.SaldoInteres,
    --    AP.Pagado,
    --    AP.InteresPagado,
    --    AP.MontoInteres,
    --    AP.MontoCreditoTotal,
    --    dbo.ufnCalcularDiasAtrazo(C.FechaVencimiento, @FechaAct)              AS Mora,
    --    AP.CuotaNro1                                                            AS Cuota
    --FROM CREDITO.Credito C
    --INNER JOIN MAESTRO.Persona P ON C.PersonaId = P.PersonaId
    --INNER JOIN AGG_PP AP         ON C.CreditoId = AP.CreditoId
    --WHERE C.Estado = 'DES'
    --  AND C.OficinaId    = ISNULL(@OficinaId, C.OficinaId)
    --  AND C.UsuarioRegId = ISNULL(@UsuarioId, C.UsuarioRegId)

    --UNION ALL

    -- Creditos PAGADOS dentro del rango de fechas
    SELECT
        C.CreditoId,        
        0                                                                       AS DiasAtrazo,
        0                                                                       AS NroCuotasPen,
        0.0                                                                       AS Saldo,
        0.0                                                                       AS SaldoCapital,
        0.0                                                                       AS SaldoInteres,
        AP.Pagado,
        AP.InteresPagado,
        AP.MontoInteres,
        AP.MontoCreditoTotal,
        CONVERT(DECIMAL(10,2), AP.TotalImporteMora)                            AS Mora,
        AP.CuotaNro1                                                            AS Cuota
    FROM CREDITO.Credito C
    INNER JOIN MAESTRO.Persona P ON C.PersonaId = P.PersonaId
    INNER JOIN AGG_PP AP         ON C.CreditoId = AP.CreditoId
    WHERE C.Estado = 'PAG'
      AND C.FechaPrimerPago BETWEEN @FechaIni AND @FechaFin
      AND C.OficinaId    = ISNULL(@OficinaId, C.OficinaId)
      AND C.UsuarioRegId = ISNULL(@UsuarioId, C.UsuarioRegId)
)
SELECT
    ROW_NUMBER() OVER (ORDER BY C.Estado, A.NombreCompleto,P.NombreCompleto)          AS Nro,
    CASE C.Estado WHEN 'DES' THEN 'DESEMBOLSADO' WHEN 'PAG' THEN 'PAGADO' END AS Estado,
    A.NombreCompleto                                                            AS Agente,
    C.CreditoId,
    P.NombreCompleto                                                            AS Cliente,
    C.MontoCredito,
    C.FormaPago,
    C.NumeroCuotas,
    C.Interes,
    CM.MontoInteres,
    CM.MontoCreditoTotal,
    C.MontoGastosAdm,
    C.CentralRiesgo,
    C.FechaPrimerPago,
    C.FechaVencimiento,
    CM.Cuota,
    (C.NumeroCuotas - CM.NroCuotasPen)                                         AS NroCuotasPagado,
    CM.Pagado,
    ISNULL(CM.InteresPagado, 0)                                                AS InteresPagado,
    CM.NroCuotasPen,
    CM.SaldoCapital,
    CM.SaldoInteres,
    CM.Saldo,
    CM.DiasAtrazo,
    CONVERT(DECIMAL(10,2), CM.Mora)                                            AS Mora
FROM CREDITO.Credito C
INNER JOIN DESEMBOLSOS CM    ON C.CreditoId    = CM.CreditoId
INNER JOIN MAESTRO.Persona P ON C.PersonaId    = P.PersonaId
INNER JOIN MAESTRO.Usuario u ON C.UsuarioRegId = u.UsuarioId
INNER JOIN MAESTRO.Persona A ON u.PersonaId    = A.PersonaId
ORDER BY C.Estado, A.NombreCompleto, P.NombreCompleto;

