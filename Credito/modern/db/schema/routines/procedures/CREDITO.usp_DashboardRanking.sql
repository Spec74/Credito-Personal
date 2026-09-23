

CREATE   PROC [CREDITO].[usp_DashboardRanking]
(
    @UsuarioId INT,
    @OficinaId INT,
    @FechaCorte DATE = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Hoy DATE = ISNULL(@FechaCorte, dbo.ufnFecha());

    DECLARE @InicioMes DATE =
        DATEFROMPARTS(YEAR(@Hoy), MONTH(@Hoy), 1);

    DECLARE @InicioMesSiguiente DATE =
        DATEADD(MONTH, 1, @InicioMes);

    ;WITH Analistas AS
    (
        SELECT DISTINCT
            U.UsuarioId,
            P.NombreCompleto
        FROM MAESTRO.Usuario U

        INNER JOIN MAESTRO.Persona P
            ON P.PersonaId = U.PersonaId

        INNER JOIN MAESTRO.UsuarioRol UR
            ON UR.UsuarioId = U.UsuarioId

        WHERE
            UR.OficinaId = @OficinaId
            AND UR.RolId = 6       -- ANALISTA
            AND U.Estado = 1
            AND U.NombreUsuario <> 'IRRECUPERABLE'
    ),

    Cobranza AS
    (
        SELECT
            C.UsuarioRegId AS UsuarioId,
            SUM(M.ImportePago) AS TotalCobrado

        FROM CREDITO.MovimientoCaja M

        INNER JOIN CREDITO.Credito C
            ON C.CreditoId = M.CreditoId

        WHERE
            M.Operacion = 'CUO'
            AND M.Estado = 1
            AND C.OficinaId = @OficinaId

            AND M.FechaReg >= @InicioMes
            AND M.FechaReg < @InicioMesSiguiente

        GROUP BY
            C.UsuarioRegId
    ),

    Ranking AS
    (
        SELECT
            A.UsuarioId,
            A.NombreCompleto,

            CAST(
                ISNULL(C.TotalCobrado, 0)
                AS DECIMAL(18,2)
            ) AS TotalCobrado,

            DENSE_RANK() OVER
            (
                ORDER BY ISNULL(C.TotalCobrado,0) DESC
            ) AS Posicion

        FROM Analistas A

        LEFT JOIN Cobranza C
            ON C.UsuarioId = A.UsuarioId
    )

    SELECT
        UsuarioId,
        NombreCompleto,
        TotalCobrado,
        Posicion,

        CASE
            WHEN UsuarioId = @UsuarioId THEN 1
            ELSE 0
        END AS EsUsuarioActual

    FROM Ranking

    ORDER BY
        Posicion,
        NombreCompleto;

END

