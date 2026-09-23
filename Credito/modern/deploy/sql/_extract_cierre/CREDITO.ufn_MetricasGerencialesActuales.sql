
CREATE   FUNCTION CREDITO.ufn_MetricasGerencialesActuales
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
    Saldos AS
    (
        SELECT * FROM CREDITO.ufn_SaldosCuotasGerenciales(@FechaCorte, @OficinaId)
    )
    SELECT C.UsuarioId,
           CAST(ISNULL(SUM(S.SaldoCuota),0) AS DECIMAL(18,2)) AS CapitalActual,
           COUNT(DISTINCT CASE WHEN S.SaldoCuota > 0 THEN S.PersonaId END) AS ClientesActivosActual,
           CAST(ISNULL(SUM(CASE WHEN S.FechaVencimiento < @FechaCorte
                               THEN S.SaldoCuota ELSE 0 END),0) AS DECIMAL(18,2)) AS VencidosActual,
           COUNT(DISTINCT CASE WHEN S.FechaVencimiento < @FechaCorte AND S.SaldoCuota > 0
                               THEN S.PersonaId END) AS ClientesVencidosActual
    FROM Carteras C
    LEFT JOIN Saldos S ON S.UsuarioId = C.UsuarioId
    GROUP BY C.UsuarioId
);

