
/* Hechos mensuales. No usa el estado de la cartera para calcular saldos. */
CREATE   FUNCTION CREDITO.ufn_ActividadGerencialMensual
(
    @Periodo DATE,
    @FechaCorte DATE,
    @OficinaId INT
)
RETURNS TABLE
AS
RETURN
(
    WITH Parametros AS
    (
        SELECT DATEFROMPARTS(YEAR(@Periodo), MONTH(@Periodo), 1) AS FechaInicio,
               DATEADD(DAY, 1, @FechaCorte) AS FechaFinExclusiva
    ),
    Carteras AS
    (
        SELECT M.UsuarioId
        FROM CREDITO.MetaGerencialAnalista M
        WHERE M.Periodo = DATEFROMPARTS(YEAR(@Periodo), MONTH(@Periodo), 1)
          AND M.Activo = 1
    ),
    ClientesNuevos AS
    (
        SELECT C.UsuarioRegId AS UsuarioId,
               COUNT(DISTINCT C.PersonaId) AS ClientesNuevosMes,
               CAST(ISNULL(SUM(C.MontoDesembolso), 0) AS DECIMAL(18,2))
                   AS MontoClientesNuevosMes
        FROM CREDITO.Credito C
        INNER JOIN MAESTRO.Cliente CL ON CL.PersonaId = C.PersonaId
        CROSS JOIN Parametros P
        WHERE CL.FechaRegistro >= P.FechaInicio
          AND CL.FechaRegistro < DATEADD(MONTH, 1, P.FechaInicio)
          AND C.FechaDesembolso >= P.FechaInicio
          AND C.FechaDesembolso < P.FechaFinExclusiva
          AND C.OficinaId = @OficinaId
          AND C.UsuarioRegId IS NOT NULL
          AND C.MontoDesembolso > 0
          AND C.Estado IN ('DES', 'PAG')
        GROUP BY C.UsuarioRegId
    ),
    Cobranza AS
    (
        SELECT C.UsuarioRegId AS UsuarioId,
               CAST(ISNULL(SUM(M.ImportePago), 0) AS DECIMAL(18,2)) AS MontoCobradoMes
        FROM CREDITO.MovimientoCaja M
        INNER JOIN CREDITO.Credito C ON C.CreditoId = M.CreditoId
        CROSS JOIN Parametros P
        WHERE M.FechaReg >= P.FechaInicio
          AND M.FechaReg < P.FechaFinExclusiva
          AND M.Operacion = 'CUO'
          AND M.Estado = 1
          AND M.IndEntrada = 1
          AND M.ImportePago > 0
          AND C.OficinaId = @OficinaId
          AND C.UsuarioRegId IS NOT NULL
        GROUP BY C.UsuarioRegId
    ),
    Desembolsos AS
    (
        SELECT C.UsuarioRegId AS UsuarioId,
               CAST(ISNULL(SUM(C.MontoDesembolso), 0) AS DECIMAL(18,2)) AS DesembolsosMes,
               COUNT(*) AS NroOperacionesMes
        FROM CREDITO.Credito C
        CROSS JOIN Parametros P
        WHERE C.FechaDesembolso >= P.FechaInicio
          AND C.FechaDesembolso < P.FechaFinExclusiva
          AND C.OficinaId = @OficinaId
          AND C.UsuarioRegId IS NOT NULL
          AND C.MontoDesembolso > 0
          AND C.Estado IN ('DES', 'PAG')
        GROUP BY C.UsuarioRegId
    )
    SELECT K.UsuarioId,
           ISNULL(N.ClientesNuevosMes, 0) AS ClientesNuevosMes,
           CAST(ISNULL(N.MontoClientesNuevosMes, 0) AS DECIMAL(18,2))
               AS MontoClientesNuevosMes,
           CAST(ISNULL(B.MontoCobradoMes, 0) AS DECIMAL(18,2)) AS MontoCobradoMes,
           CAST(ISNULL(D.DesembolsosMes, 0) AS DECIMAL(18,2)) AS DesembolsosMes,
           ISNULL(D.NroOperacionesMes, 0) AS NroOperacionesMes
    FROM Carteras K
    LEFT JOIN ClientesNuevos N ON N.UsuarioId = K.UsuarioId
    LEFT JOIN Cobranza B ON B.UsuarioId = K.UsuarioId
    LEFT JOIN Desembolsos D ON D.UsuarioId = K.UsuarioId
);

