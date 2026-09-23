
CREATE   FUNCTION CREDITO.ufn_RecuperacionVencidosGerencial
(
    @Periodo DATE,
    @FechaCorte DATE
)
RETURNS TABLE
AS
RETURN
(
    WITH Apertura AS
    (
        SELECT A.UsuarioId, A.CreditoId, A.PlanPagoId, A.SaldoVencidoApertura
        FROM CREDITO.VencidoGerencialAperturaDetalle A
        WHERE A.Periodo = DATEFROMPARTS(YEAR(@Periodo), MONTH(@Periodo), 1)
    ),
    CreditosApertura AS
    (
        SELECT DISTINCT A.CreditoId FROM Apertura A
    ),
    Pagos AS
    (
        SELECT M.CreditoId, SUM(M.ImportePago) AS TotalPagado
        FROM CREDITO.MovimientoCaja M
        INNER JOIN CreditosApertura C ON C.CreditoId = M.CreditoId
        WHERE M.Operacion = 'CUO' AND M.Estado = 1 AND M.IndEntrada = 1
          AND M.ImportePago > 0 AND M.FechaReg < DATEADD(DAY,1,@FechaCorte)
        GROUP BY M.CreditoId
    ),
    Cuotas AS
    (
        SELECT P.CreditoId, P.PlanPagoId,
               CAST(P.Cuota + P.Cargo AS DECIMAL(18,2)) AS ImporteCuota,
               CAST(ISNULL(G.TotalPagado,0) AS DECIMAL(18,2)) AS TotalPagado,
               CAST(ISNULL(SUM(P.Cuota + P.Cargo) OVER
                    (PARTITION BY P.CreditoId ORDER BY P.FechaVencimiento,P.PlanPagoId
                     ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING),0)
                    AS DECIMAL(18,2)) AS AcumuladoAnterior,
               CAST(SUM(P.Cuota + P.Cargo) OVER
                    (PARTITION BY P.CreditoId ORDER BY P.FechaVencimiento,P.PlanPagoId
                     ROWS UNBOUNDED PRECEDING) AS DECIMAL(18,2)) AS AcumuladoIncluido
        FROM CREDITO.PlanPago P
        INNER JOIN CreditosApertura C ON C.CreditoId = P.CreditoId
        LEFT JOIN Pagos G ON G.CreditoId = P.CreditoId
    ),
    Saldos AS
    (
        SELECT Q.PlanPagoId,
               CAST(CASE WHEN Q.TotalPagado >= Q.AcumuladoIncluido THEN 0
                         WHEN Q.TotalPagado <= Q.AcumuladoAnterior THEN Q.ImporteCuota
                         ELSE Q.AcumuladoIncluido - Q.TotalPagado END
                    AS DECIMAL(18,2)) AS SaldoActual
        FROM Cuotas Q
    ),
    Comparacion AS
    (
        SELECT A.UsuarioId, A.SaldoVencidoApertura,
               CAST(CASE WHEN ISNULL(S.SaldoActual,0) <= 0 THEN 0
                         WHEN S.SaldoActual >= A.SaldoVencidoApertura
                              THEN A.SaldoVencidoApertura
                         ELSE S.SaldoActual END AS DECIMAL(18,2)) AS SaldoPendienteBase
        FROM Apertura A
        LEFT JOIN Saldos S ON S.PlanPagoId = A.PlanPagoId
    )
    SELECT C.UsuarioId,
           CAST(SUM(C.SaldoVencidoApertura) AS DECIMAL(18,2)) AS VencidosApertura,
           CAST(SUM(C.SaldoPendienteBase) AS DECIMAL(18,2)) AS VencidosAperturaPendiente,
           CAST(SUM(C.SaldoVencidoApertura-C.SaldoPendienteBase) AS DECIMAL(18,2))
                AS RecuperacionVencidosActual
    FROM Comparacion C GROUP BY C.UsuarioId
);

