-- Morosidad empresarial (modulo /morosidad).
-- Aplicar en Azure SQL Database (base CREDITO) antes de usar el informe.
-- CREATE OR ALTER: sirve si el procedimiento aun no existe.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_MorosidadEmpresa]
(
    @Tipo        VARCHAR(30) = NULL,
    @OficinaId   INT = NULL,
    @UsuarioId   INT = NULL,
    @FechaCorte  DATE = NULL
)
WITH RECOMPILE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Hoy DATE = ISNULL(@FechaCorte, dbo.ufnFecha());
    DECLARE @Manana DATE = DATEADD(DAY, 1, @Hoy);

    SET @Tipo = UPPER(LTRIM(RTRIM(ISNULL(@Tipo, 'TODOS'))));

    IF @Tipo = ''
        SET @Tipo = 'TODOS';

    IF @Tipo NOT IN
    (
        'TODOS',
        'SIN_PAGO',
        'NUNCA_PAGO',
        'DEJO_PAGAR',
        'PAGA_CON_ATRASO'
    )
    BEGIN
        THROW 50001, 'El tipo de morosidad solicitado no es valido.', 1;
    END;

    IF @OficinaId IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM MAESTRO.Oficina O
           WHERE O.OficinaId = @OficinaId
       )
    BEGIN
        THROW 50002, 'La oficina solicitada no existe.', 1;
    END;

    IF @UsuarioId IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM MAESTRO.Usuario U
           WHERE U.UsuarioId = @UsuarioId
       )
    BEGIN
        THROW 50003, 'El gestor solicitado no existe.', 1;
    END;

    ;WITH CreditosActivos AS
    (
        SELECT
            C.CreditoId,
            C.PersonaId,
            C.OficinaId,
            C.UsuarioRegId AS GestorId
        FROM CREDITO.Credito C
        WHERE C.Estado = 'DES'
          AND ISNULL(C.IndIrrecuperable, 0) = 0
          AND C.PersonaId IS NOT NULL
          AND C.FechaDesembolso < @Manana
          AND (@OficinaId IS NULL OR C.OficinaId = @OficinaId)
          AND (@UsuarioId IS NULL OR C.UsuarioRegId = @UsuarioId)
    ),
    PlanAgregado AS
    (
        SELECT
            PP.CreditoId,
            SUM(ISNULL(PP.Cuota, 0) + ISNULL(PP.Cargo, 0)) AS MontoPlan,
            MAX
            (
                CASE
                    WHEN PP.Estado = 'PEN'
                     AND PP.FechaVencimiento < @Hoy
                    THEN 1 ELSE 0
                END
            ) AS TieneMora,
            MIN
            (
                CASE
                    WHEN PP.Estado = 'PEN'
                     AND PP.FechaVencimiento < @Hoy
                    THEN PP.FechaVencimiento
                END
            ) AS PrimeraCuotaVencida
        FROM CREDITO.PlanPago PP
        INNER JOIN CreditosActivos CA
            ON CA.CreditoId = PP.CreditoId
        GROUP BY PP.CreditoId
    ),
    PagosAgregados AS
    (
        SELECT
            M.CreditoId,
            SUM(M.ImportePago) AS TotalPagado,
            MAX(M.FechaReg) AS FechaUltimoPago
        FROM CREDITO.MovimientoCaja M
        INNER JOIN CreditosActivos CA
            ON CA.CreditoId = M.CreditoId
        WHERE M.Operacion = 'CUO'
          AND M.Estado = 1
          AND M.ImportePago > 0
          AND M.FechaReg < @Manana
        GROUP BY M.CreditoId
    ),
    CarteraCredito AS
    (
        SELECT
            CA.CreditoId,
            CA.PersonaId,
            CA.OficinaId,
            CA.GestorId,
            ISNULL(PA.TieneMora, 0) AS TieneMora,
            PA.PrimeraCuotaVencida,
            CAST
            (
                CASE
                    WHEN ISNULL(PA.MontoPlan, 0) - ISNULL(PG.TotalPagado, 0) > 0
                    THEN ISNULL(PA.MontoPlan, 0) - ISNULL(PG.TotalPagado, 0)
                    ELSE 0
                END
                AS DECIMAL(18, 2)
            ) AS Saldo,
            CAST(ISNULL(PG.TotalPagado, 0) AS DECIMAL(18, 2)) AS TotalPagado,
            PG.FechaUltimoPago
        FROM CreditosActivos CA
        LEFT JOIN PlanAgregado PA
            ON PA.CreditoId = CA.CreditoId
        LEFT JOIN PagosAgregados PG
            ON PG.CreditoId = CA.CreditoId
    ),
    CarteraPersona AS
    (
        SELECT
            PersonaId,
            COUNT(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN 1 END) AS CreditosMora,
            SUM(CASE WHEN TieneMora = 1 THEN Saldo ELSE 0 END) AS SaldoMora,
            MIN(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN PrimeraCuotaVencida END)
                AS PrimeraCuotaVencida,
            MAX(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN FechaUltimoPago END)
                AS FechaUltimoPago,
            SUM(CASE WHEN TieneMora = 1 THEN TotalPagado ELSE 0 END)
                AS PagadoEnCreditosMora,
            MAX
            (
                CASE
                    WHEN TieneMora = 1
                     AND Saldo > 0
                     AND FechaUltimoPago >= PrimeraCuotaVencida
                    THEN 1 ELSE 0
                END
            ) AS PagoDesdeInicioMora,
            MIN(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN OficinaId END)
                AS OficinaIdMin,
            MAX(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN OficinaId END)
                AS OficinaIdMax,
            MIN(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN GestorId END)
                AS GestorIdMin,
            MAX(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN GestorId END)
                AS GestorIdMax
        FROM CarteraCredito
        GROUP BY PersonaId
    ),
    Clasificados AS
    (
        SELECT
            CP.PersonaId,
            CP.CreditosMora,
            CAST(CP.SaldoMora AS DECIMAL(18, 2)) AS SaldoMora,
            CP.PrimeraCuotaVencida,
            CP.FechaUltimoPago,
            CASE
                WHEN CP.PrimeraCuotaVencida IS NULL THEN 0
                ELSE
                    DATEDIFF(DAY, CP.PrimeraCuotaVencida, @Hoy)
                    - DATEDIFF
                      (
                          WEEK,
                          DATEADD(DAY, -1, CP.PrimeraCuotaVencida),
                          @Hoy
                      )
            END AS DiasAtraso,
            CAST
            (
                CASE
                    WHEN CP.PagadoEnCreditosMora <= 0 THEN 'NUNCA_PAGO'
                    WHEN CP.PagoDesdeInicioMora = 0 THEN 'DEJO_PAGAR'
                    ELSE 'PAGA_CON_ATRASO'
                END
                AS VARCHAR(30)
            ) AS CodigoClasificacion,
            CASE
                WHEN CP.OficinaIdMin = CP.OficinaIdMax THEN CP.OficinaIdMin
                ELSE NULL
            END AS OficinaId,
            CASE
                WHEN CP.GestorIdMin = CP.GestorIdMax THEN CP.GestorIdMin
                ELSE NULL
            END AS GestorId
        FROM CarteraPersona CP
        WHERE CP.CreditosMora > 0
          AND CP.SaldoMora > 0
    )
    SELECT
        CAST(C.PersonaId AS INT) AS PersonaId,
        CAST(P.NombreCompleto AS VARCHAR(250)) AS NombreCompleto,
        CAST(P.NumeroDocumento AS VARCHAR(12)) AS NumeroDocumento,
        CAST(P.Celular1 AS VARCHAR(10)) AS Celular,
        CAST(C.OficinaId AS INT) AS OficinaId,
        CAST
        (
            CASE
                WHEN C.OficinaId IS NULL THEN 'VARIAS OFICINAS'
                ELSE ISNULL(O.Denominacion, 'OFICINA SIN NOMBRE')
            END
            AS VARCHAR(100)
        ) AS Oficina,
        CAST(C.GestorId AS INT) AS GestorId,
        CAST
        (
            CASE
                WHEN C.GestorId IS NULL THEN 'VARIOS'
                ELSE ISNULL(U.NombreUsuario, 'SIN USUARIO')
            END
            AS NVARCHAR(50)
        ) AS GestorUsuario,
        CAST
        (
            CASE
                WHEN C.GestorId IS NULL THEN 'VARIOS GESTORES'
                ELSE ISNULL(PG.NombreCompleto, U.NombreUsuario)
            END
            AS NVARCHAR(250)
        ) AS GestorNombre,
        CAST(C.CreditosMora AS INT) AS CreditosMora,
        CAST(C.SaldoMora AS DECIMAL(18, 2)) AS SaldoMora,
        CAST(C.PrimeraCuotaVencida AS DATETIME) AS PrimeraCuotaVencida,
        CAST(C.FechaUltimoPago AS DATETIME) AS FechaUltimoPago,
        CAST(C.DiasAtraso AS INT) AS DiasAtraso,
        CAST(C.CodigoClasificacion AS VARCHAR(30)) AS CodigoClasificacion,
        CAST
        (
            CASE C.CodigoClasificacion
                WHEN 'NUNCA_PAGO' THEN 'Nunca pagó'
                WHEN 'DEJO_PAGAR' THEN 'Dejó de pagar'
                ELSE 'Paga con atraso'
            END
            AS VARCHAR(30)
        ) AS Clasificacion,
        CAST(@Hoy AS DATETIME) AS FechaCorte
    FROM Clasificados C
    INNER JOIN MAESTRO.Persona P
        ON P.PersonaId = C.PersonaId
    LEFT JOIN MAESTRO.Oficina O
        ON O.OficinaId = C.OficinaId
    LEFT JOIN MAESTRO.Usuario U
        ON U.UsuarioId = C.GestorId
    LEFT JOIN MAESTRO.Persona PG
        ON PG.PersonaId = U.PersonaId
    WHERE @Tipo = 'TODOS'
       OR
       (
           @Tipo = 'SIN_PAGO'
           AND C.CodigoClasificacion IN ('NUNCA_PAGO', 'DEJO_PAGAR')
       )
       OR C.CodigoClasificacion = @Tipo
    ORDER BY
        CASE C.CodigoClasificacion
            WHEN 'NUNCA_PAGO' THEN 1
            WHEN 'DEJO_PAGAR' THEN 2
            ELSE 3
        END,
        C.DiasAtraso DESC,
        C.SaldoMora DESC,
        P.NombreCompleto;
END
GO
