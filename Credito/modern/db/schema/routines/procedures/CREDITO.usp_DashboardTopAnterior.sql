
CREATE   PROC [CREDITO].[usp_DashboardTopAnterior]
(
    @OficinaId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Hoy DATE = dbo.ufnFecha();

    DECLARE @InicioMesActual DATE =
        DATEFROMPARTS(YEAR(@Hoy), MONTH(@Hoy), 1);

    DECLARE @InicioMesAnterior DATE =
        DATEADD(MONTH, -1, @InicioMesActual);

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
        WHERE UR.OficinaId = @OficinaId
          AND UR.RolId = 6
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
        WHERE M.Operacion = 'CUO'
          AND M.Estado = 1
          AND C.OficinaId = @OficinaId
          AND M.FechaReg >= @InicioMesAnterior
          AND M.FechaReg < @InicioMesActual
        GROUP BY C.UsuarioRegId
    )
    SELECT TOP 3
        A.UsuarioId,
        A.NombreCompleto,
        CAST(ISNULL(C.TotalCobrado, 0) AS DECIMAL(18,2)) AS TotalCobrado,
        CAST(
            ROW_NUMBER() OVER
            (
                ORDER BY ISNULL(C.TotalCobrado, 0) DESC
            )
            AS INT
        ) AS Posicion
    FROM Analistas A
    LEFT JOIN Cobranza C
        ON C.UsuarioId = A.UsuarioId
    WHERE ISNULL(C.TotalCobrado, 0) > 0
    ORDER BY TotalCobrado DESC, A.NombreCompleto;
END

