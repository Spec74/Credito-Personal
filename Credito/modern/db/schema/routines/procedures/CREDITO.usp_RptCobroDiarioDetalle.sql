
/*

[CREDITO].[usp_RptCobroDiario] 11

[CREDITO].[usp_RptCobroDiarioDetalle] 1023

*/
CREATE PROC [CREDITO].[usp_RptCobroDiarioDetalle]
    @UsuarioId INT = NULL,
    @OficinaId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @FechaAct DATE = dbo.ufnFecha();

    SELECT
        ROW_NUMBER() OVER (ORDER BY P.NombreCompleto) AS Nro,
        P.NombreCompleto AS Cliente,
        C.FormaPago,
        C.MontoCredito,
        C.Interes,
        PL.MontoTotal,
        C.FechaPrimerPago,
        C.FechaVencimiento,
        (ISNULL(PL.MontoTotal, 0) - ISNULL(PD.TotalPago, 0)) AS Saldo,
        PD.TotalPago,
        dbo.ufnCalcularDiasAtrazo(C.FechaVencimiento, @FechaAct) AS DiasAtrazoMora,
        PD.Pagos
    FROM CREDITO.Credito C
    INNER JOIN MAESTRO.Persona P ON C.PersonaId = P.PersonaId
    LEFT JOIN (
        SELECT CreditoId, SUM(Cuota + Cargo) AS MontoTotal
        FROM CREDITO.PlanPago
        GROUP BY CreditoId
    ) PL ON C.CreditoId = PL.CreditoId
    LEFT JOIN (
        SELECT 
            CreditoId,
            STRING_AGG(CAST(ImportePago AS VARCHAR(20)) + ' (' + CONVERT(VARCHAR(10), FechaReg, 103) + ')', ', ') AS Pagos,
            SUM(ImportePago) AS TotalPago
        FROM CREDITO.MovimientoCaja
        WHERE Operacion = 'CUO' AND Estado = 1
        GROUP BY CreditoId
    ) PD ON C.CreditoId = PD.CreditoId
    WHERE C.Estado = 'DES'
      AND (@OficinaId IS NULL OR C.OficinaId = @OficinaId)
      AND (@UsuarioId IS NULL OR C.UsuarioRegId = @UsuarioId);
END
