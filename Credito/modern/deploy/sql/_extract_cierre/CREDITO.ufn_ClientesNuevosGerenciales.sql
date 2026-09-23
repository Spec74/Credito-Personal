
CREATE   FUNCTION CREDITO.ufn_ClientesNuevosGerenciales
(
    @Periodo DATE,
    @OficinaId INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT C.UsuarioRegId AS UsuarioId,
           COUNT(DISTINCT C.PersonaId) AS ClientesNuevosMes
    FROM CREDITO.Credito C
    INNER JOIN MAESTRO.Cliente CL ON CL.PersonaId = C.PersonaId
    WHERE CL.FechaRegistro >= DATEFROMPARTS(YEAR(@Periodo), MONTH(@Periodo), 1)
      AND CL.FechaRegistro < DATEADD(MONTH, 1,
            DATEFROMPARTS(YEAR(@Periodo), MONTH(@Periodo), 1))
      AND C.OficinaId = @OficinaId AND C.UsuarioRegId IS NOT NULL
    GROUP BY C.UsuarioRegId
);

