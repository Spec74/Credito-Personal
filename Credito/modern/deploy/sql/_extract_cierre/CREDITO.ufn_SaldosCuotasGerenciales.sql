
/* Saldo FIFO de todas las cuotas de los creditos activos. */
CREATE   FUNCTION CREDITO.ufn_SaldosCuotasGerenciales
(
    @FechaCorte DATE,
    @OficinaId INT
)
RETURNS TABLE
AS
RETURN
(
    WITH Carteras AS
    (
        SELECT M.UsuarioId
        FROM CREDITO.MetaGerencialAnalista M
        WHERE M.Periodo = DATEFROMPARTS(YEAR(@FechaCorte), MONTH(@FechaCorte), 1)
          AND M.Activo = 1
    ),
    CreditosActuales AS
    (
        SELECT C.CreditoId, C.UsuarioRegId AS UsuarioId, C.PersonaId
        FROM CREDITO.Credito C
        INNER JOIN Carteras A ON A.UsuarioId = C.UsuarioRegId
        WHERE C.OficinaId = @OficinaId AND C.Estado = 'DES'
    ),
    Pagos AS
    (
        SELECT M.CreditoId, SUM(M.ImportePago) AS TotalPagado
        FROM CREDITO.MovimientoCaja M
        INNER JOIN CreditosActuales C ON C.CreditoId = M.CreditoId
        WHERE M.Operacion = 'CUO' AND M.Estado = 1 AND M.IndEntrada = 1
          AND M.ImportePago > 0 AND M.FechaReg < DATEADD(DAY, 1, @FechaCorte)
        GROUP BY M.CreditoId
    ),
    Cuotas AS
    (
        SELECT C.UsuarioId, C.PersonaId, C.CreditoId, P.PlanPagoId,
               P.FechaVencimiento,
               CAST(P.Cuota + P.Cargo AS DECIMAL(18,2)) AS ImporteCuota,
               CAST(ISNULL(G.TotalPagado,0) AS DECIMAL(18,2)) AS TotalPagado,
               CAST(ISNULL(SUM(P.Cuota + P.Cargo) OVER
                    (PARTITION BY P.CreditoId ORDER BY P.FechaVencimiento, P.PlanPagoId
                     ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING),0)
                    AS DECIMAL(18,2)) AS AcumuladoAnterior,
               CAST(SUM(P.Cuota + P.Cargo) OVER
                    (PARTITION BY P.CreditoId ORDER BY P.FechaVencimiento, P.PlanPagoId
                     ROWS UNBOUNDED PRECEDING) AS DECIMAL(18,2)) AS AcumuladoIncluido
        FROM CreditosActuales C
        INNER JOIN CREDITO.PlanPago P ON P.CreditoId = C.CreditoId
        LEFT JOIN Pagos G ON G.CreditoId = C.CreditoId
    )
    SELECT Q.UsuarioId, Q.PersonaId, Q.CreditoId, Q.PlanPagoId,
           Q.FechaVencimiento, Q.ImporteCuota,
           CAST(CASE WHEN Q.TotalPagado >= Q.AcumuladoIncluido THEN 0
                     WHEN Q.TotalPagado <= Q.AcumuladoAnterior THEN Q.ImporteCuota
                     ELSE Q.AcumuladoIncluido - Q.TotalPagado END
                AS DECIMAL(18,2)) AS SaldoCuota
    FROM Cuotas Q
);

