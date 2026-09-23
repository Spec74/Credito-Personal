



CREATE PROCEDURE [CREDITO].[usp_RptCobroDiario]
    @UsuarioId INT = NULL,
    @OficinaId INT = NULL,
    @CreditoId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FechaAct DATE = dbo.ufnFecha();


    CREATE TABLE #CreditosFiltrados
    (
        CreditoId INT NOT NULL PRIMARY KEY
    );

    INSERT INTO #CreditosFiltrados (CreditoId)
    SELECT C.CreditoId
    FROM CREDITO.Credito C
    WHERE C.Estado = 'DES'
        AND (@UsuarioId IS NULL OR C.UsuarioRegId = @UsuarioId)
        AND (@OficinaId IS NULL OR C.OficinaId = @OficinaId)
        AND (@CreditoId IS NULL OR C.CreditoId = @CreditoId)
    OPTION (RECOMPILE);


    ;WITH PlanPagoAgregado AS
    (
        SELECT
            PP.CreditoId,

            MIN(
                CASE
                    WHEN PP.Estado = 'PEN'
                    THEN PP.FechaVencimiento
                END
            ) AS MinFechaVencimiento,

            MIN(
                CASE
                    WHEN PP.Estado = 'PEN'
                     AND PP.FechaVencimiento >= @FechaAct
                    THEN PP.FechaVencimiento
                END
            ) AS FechaProximoPago,

            COUNT(
                CASE
                    WHEN PP.Estado = 'PEN'
                     AND PP.FechaVencimiento < @FechaAct
                    THEN 1
                END
            ) AS NroCuotasPen,

            SUM(
                CASE
                    WHEN PP.Estado = 'PEN'
                     AND PP.FechaVencimiento < @FechaAct
                    THEN PP.Cuota + PP.Cargo - PP.PagoLibre
                    ELSE 0
                END
            ) AS CuotaAcumulada,

            SUM(
                CASE
                    WHEN PP.Estado = 'PEN'
                    THEN PP.Cuota
                    ELSE 0
                END
            ) AS SumaCuota,

            SUM(PP.Cuota + PP.Cargo) AS MontoTotal,
            MIN(PP.Cuota) AS CuotaPlan

        FROM CREDITO.PlanPago PP

        INNER JOIN #CreditosFiltrados CF
            ON CF.CreditoId = PP.CreditoId

        GROUP BY PP.CreditoId
    ),

    CuotaPendienteRanked AS
    (
        SELECT
            PP.CreditoId,
            PP.Cuota + PP.Cargo - PP.PagoLibre AS CuotaPendiente,

            ROW_NUMBER() OVER
            (
                PARTITION BY PP.CreditoId
                ORDER BY PP.Numero
            ) AS rn

        FROM CREDITO.PlanPago PP

        INNER JOIN #CreditosFiltrados CF
            ON CF.CreditoId = PP.CreditoId

        WHERE PP.Estado = 'PEN'
    ),

    MovimientoCajaAgregado AS
    (
        SELECT
            MC.CreditoId,

            SUM(ISNULL(MC.ImportePago, 0)) AS MovCaja,

            CAST(
                MAX(
                    CASE
                        WHEN ISNULL(MC.ImportePago, 0) > 0
                        THEN MC.FechaReg
                    END
                ) AS DATE
            ) AS FechaUltimoPago,

            MAX(
                CASE
                    WHEN ISNULL(MC.ImportePago, 0) > 0
                    THEN 1
                    ELSE 0
                END
            ) AS TienePagoReal

        FROM CREDITO.MovimientoCaja MC

        INNER JOIN #CreditosFiltrados CF
            ON CF.CreditoId = MC.CreditoId

        WHERE MC.Operacion = 'CUO'
          AND MC.Estado = 1

        GROUP BY MC.CreditoId
    ),

    DESEMBOLSOS AS
    (
        SELECT
            C.CreditoId,

            TRY_CAST(
                SUBSTRING(P.Codigo, 3, LEN(P.Codigo))
                AS INT
            ) AS Orden,

            dbo.ufnCalcularDiasAtrazo(
                PPA.MinFechaVencimiento,
                @FechaAct
            ) AS DiasAtrazo,

            MCA.FechaUltimoPago,
            PPA.FechaProximoPago,

            PPA.MinFechaVencimiento,

            -- También devuelve 0 cuando no existe ningún movimiento.
            ISNULL(MCA.TienePagoReal, 0) AS TienePagoReal,

            PPA.NroCuotasPen,
            PPA.CuotaPlan,
            NULLIF(PPA.CuotaAcumulada, 0) AS CuotaAcumulada,
            CPR.CuotaPendiente,
            PPA.SumaCuota,
            ISNULL(MCA.MovCaja, 0) AS MovCaja,
            PPA.MontoTotal,

            dbo.ufnCalcularDiasAtrazo(
                C.FechaVencimiento,
                @FechaAct
            ) AS DiasAtrazoMora

        FROM CREDITO.Credito C

        INNER JOIN #CreditosFiltrados CF
            ON CF.CreditoId = C.CreditoId

        INNER JOIN MAESTRO.Persona P
            ON C.PersonaId = P.PersonaId

        LEFT JOIN PlanPagoAgregado PPA
            ON C.CreditoId = PPA.CreditoId

        LEFT JOIN CuotaPendienteRanked CPR
            ON C.CreditoId = CPR.CreditoId
           AND CPR.rn = 1

        LEFT JOIN MovimientoCajaAgregado MCA
            ON C.CreditoId = MCA.CreditoId

    )

    SELECT
        ROW_NUMBER() OVER (
            ORDER BY P.NombreCompleto
        ) AS Nro,

        CM.Orden,
        C.CreditoId,
        P.NombreCompleto AS Cliente,
        P.Celular1 AS Celular,
        C.MontoCredito,
        C.Interes,
        CM.CuotaPlan,

        CM.MontoTotal - CM.MovCaja AS Saldo,

        CM.DiasAtrazo,
        CM.NroCuotasPen,

        ISNULL(
            CM.CuotaAcumulada,
            CM.CuotaPendiente
        ) AS CuotaTotal,

        P.Direccion,

        CASE
            -- Crédito vencido:
            -- si pagó, mostrar su último pago real.
            WHEN C.FechaVencimiento < @FechaAct
                 AND CM.TienePagoReal = 1
            THEN CM.FechaUltimoPago

            -- Crédito vencido sin ningún pago real:
            -- mostrar la cuota pendiente más antigua.
            WHEN C.FechaVencimiento < @FechaAct
                 AND CM.TienePagoReal = 0
            THEN COALESCE(
                CM.MinFechaVencimiento,
                C.FechaPrimerPago
            )

            -- Crédito vigente sin pagos:
            -- mostrar la próxima fecha programada.
            WHEN CM.TienePagoReal = 0
            THEN COALESCE(
                CM.FechaProximoPago,
                CASE
                    WHEN C.FechaPrimerPago >= @FechaAct
                    THEN C.FechaPrimerPago
                END
            )

            -- Crédito vigente semanal, quincenal o mensual con pagos:
            -- mostrar la próxima fecha programada.
            WHEN UPPER(LTRIM(RTRIM(C.FormaPago))) IN
                 ('S', 'SEMANAL', 'Q', 'QUINCENAL', 'M', 'MENSUAL')
            THEN CM.FechaProximoPago

            -- Crédito diario vigente con pagos:
            -- mostrar el último pago real.
            ELSE CM.FechaUltimoPago
        END AS FechaPago,

        CAST(CM.TienePagoReal AS BIT) AS TienePagoReal,

        C.FechaPrimerPago,
        C.FechaVencimiento,

        CONVERT(
            DECIMAL(10, 2),
            dbo.ufnCalcularMora(
                CM.SumaCuota,
                CM.DiasAtrazoMora,
                0
            )
        ) AS Mora,

        CM.MontoTotal,
        O.Denominacion AS Negocio,
        C.FormaPago,
        CL.TopeCredito,
        SBS.DesCorta AS ClasificacionRiesgoSBS

    FROM CREDITO.Credito C

    INNER JOIN DESEMBOLSOS CM
        ON C.CreditoId = CM.CreditoId

    INNER JOIN MAESTRO.Persona P
        ON C.PersonaId = P.PersonaId

    INNER JOIN MAESTRO.Cliente CL
        ON C.PersonaId = CL.ClienteId

    LEFT JOIN MAESTRO.ValorTabla SBS
        ON SBS.TablaId = 14
       AND CL.ClasificacionRiesgoSBS = SBS.ItemId

    LEFT JOIN MAESTRO.Ocupacion O
        ON CL.ActividadEconId = O.OcupacionId;
END;

