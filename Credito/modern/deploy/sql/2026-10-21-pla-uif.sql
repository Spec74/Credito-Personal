-- T-C7: PLA/UIF con piso legal (D.S. 020-2017-JUS art. 24.4 / 25).
-- No inventa umbral en soles. Pisos en USD. TC = promedio venta SBS mes anterior.
-- Gerencia solo puede bajar el piso (24.4.c). ROS no tiene monto.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'CREDITO.PlaParametro', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.PlaParametro (
        PlaParametroId      int NOT NULL CONSTRAINT PK_PlaParametro PRIMARY KEY,
        IndividualUsd       decimal(18, 2) NOT NULL,
        MultipleUsd         decimal(18, 2) NOT NULL,
        UmbralIndividualMenorUsd decimal(18, 2) NULL,
        UmbralMultipleMenorUsd   decimal(18, 2) NULL,
        TcVentaMesAnterior  decimal(12, 4) NULL,
        TcPeriodo           char(7) NULL,
        FuenteNorma         varchar(80) NOT NULL,
        UsuarioModId        int NULL,
        FechaMod            datetime NULL,
        CONSTRAINT CK_PlaParametro_UnaFila CHECK (PlaParametroId = 1)
    );

    INSERT INTO CREDITO.PlaParametro (
        PlaParametroId, IndividualUsd, MultipleUsd, FuenteNorma)
    VALUES (1, 10000.00, 50000.00, N'D.S. 020-2017-JUS art. 24.4');
END
GO

IF OBJECT_ID(N'CREDITO.PlaInusual', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.PlaInusual (
        PlaInusualId    int IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_PlaInusual PRIMARY KEY,
        OficinaId       int NOT NULL,
        PersonaId       int NOT NULL,
        CreditoId       int NULL,
        Calificacion    varchar(12) NOT NULL,
        Motivo          varchar(500) NOT NULL,
        UsuarioRegId    int NOT NULL,
        FechaReg        datetime NOT NULL,
        CONSTRAINT CK_PlaInusual_Calificacion
            CHECK (Calificacion IN (N'INUSUAL', N'SOSPECHOSA', N'DESCARTADA'))
    );

    CREATE INDEX IX_PlaInusual_Oficina_Fecha
        ON CREDITO.PlaInusual (OficinaId, FechaReg DESC);
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_PlaObtenerParametro]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.PlaParametroId,
        p.IndividualUsd,
        p.MultipleUsd,
        p.UmbralIndividualMenorUsd,
        p.UmbralMultipleMenorUsd,
        CAST(COALESCE(p.UmbralIndividualMenorUsd, p.IndividualUsd) AS decimal(18, 2))
            AS IndividualEfectivoUsd,
        CAST(COALESCE(p.UmbralMultipleMenorUsd, p.MultipleUsd) AS decimal(18, 2))
            AS MultipleEfectivoUsd,
        p.TcVentaMesAnterior,
        p.TcPeriodo,
        p.FuenteNorma,
        CASE WHEN p.TcVentaMesAnterior IS NULL OR p.TcVentaMesAnterior <= 0
             THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS TieneTipoCambio
    FROM CREDITO.PlaParametro AS p
    WHERE p.PlaParametroId = 1;
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_PlaGuardarParametro]
    @TcVentaMesAnterior decimal(12, 4) = NULL,
    @TcPeriodo char(7) = NULL,
    @UmbralIndividualMenorUsd decimal(18, 2) = NULL,
    @UmbralMultipleMenorUsd decimal(18, 2) = NULL,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Usuario inválido.', 16, 1);
        RETURN;
    END;

    DECLARE @IndividualUsd decimal(18, 2);
    DECLARE @MultipleUsd decimal(18, 2);

    SELECT
        @IndividualUsd = IndividualUsd,
        @MultipleUsd = MultipleUsd
    FROM CREDITO.PlaParametro
    WHERE PlaParametroId = 1;

    IF @IndividualUsd IS NULL
    BEGIN
        RAISERROR(N'Falta fila CREDITO.PlaParametro.', 16, 1);
        RETURN;
    END;

    IF @TcVentaMesAnterior IS NOT NULL AND @TcVentaMesAnterior <= 0
    BEGIN
        RAISERROR(N'El tipo de cambio SBS debe ser mayor que cero.', 16, 1);
        RETURN;
    END;

    IF @UmbralIndividualMenorUsd IS NOT NULL
       AND (@UmbralIndividualMenorUsd <= 0 OR @UmbralIndividualMenorUsd > @IndividualUsd)
    BEGIN
        RAISERROR(N'El umbral menor individual debe ser > 0 y no superar el piso legal.', 16, 1);
        RETURN;
    END;

    IF @UmbralMultipleMenorUsd IS NOT NULL
       AND (@UmbralMultipleMenorUsd <= 0 OR @UmbralMultipleMenorUsd > @MultipleUsd)
    BEGIN
        RAISERROR(N'El umbral menor múltiple debe ser > 0 y no superar el piso legal.', 16, 1);
        RETURN;
    END;

    UPDATE CREDITO.PlaParametro
    SET TcVentaMesAnterior = @TcVentaMesAnterior,
        TcPeriodo = NULLIF(LTRIM(RTRIM(@TcPeriodo)), N''),
        UmbralIndividualMenorUsd = @UmbralIndividualMenorUsd,
        UmbralMultipleMenorUsd = @UmbralMultipleMenorUsd,
        UsuarioModId = @UsuarioId,
        FechaMod = dbo.ufnFecha()
    WHERE PlaParametroId = 1;

    EXEC CREDITO.usp_PlaObtenerParametro;
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_PlaCandidatosRo]
    @OficinaId int,
    @Anio int,
    @Mes int
AS
BEGIN
    SET NOCOUNT ON;

    IF @OficinaId IS NULL OR @OficinaId < 1
       OR @Anio IS NULL OR @Anio < 2000
       OR @Mes IS NULL OR @Mes < 1 OR @Mes > 12
    BEGIN
        RAISERROR(N'Oficina y periodo inválidos.', 16, 1);
        RETURN;
    END;

    DECLARE @Desde datetime = DATEFROMPARTS(@Anio, @Mes, 1);
    DECLARE @Hasta datetime = DATEADD(MONTH, 1, @Desde);
    DECLARE @IndividualUsd decimal(18, 2);
    DECLARE @MultipleUsd decimal(18, 2);
    DECLARE @Tc decimal(12, 4);
    DECLARE @TieneTc bit;
    DECLARE @UmbralIndividualPen decimal(18, 2);
    DECLARE @UmbralMultiplePen decimal(18, 2);

    SELECT
        @IndividualUsd = COALESCE(UmbralIndividualMenorUsd, IndividualUsd),
        @MultipleUsd = COALESCE(UmbralMultipleMenorUsd, MultipleUsd),
        @Tc = TcVentaMesAnterior
    FROM CREDITO.PlaParametro
    WHERE PlaParametroId = 1;

    SET @TieneTc = CASE WHEN @Tc IS NULL OR @Tc <= 0 THEN 0 ELSE 1 END;
    SET @UmbralIndividualPen = CASE WHEN @TieneTc = 1 THEN @IndividualUsd * @Tc END;
    SET @UmbralMultiplePen = CASE WHEN @TieneTc = 1 THEN @MultipleUsd * @Tc END;

    ;WITH Ops AS (
        SELECT
            CAST(N'DESEMBOLSO' AS varchar(16)) AS Tipo,
            c.CreditoId,
            c.PersonaId,
            c.OficinaId,
            CAST(COALESCE(c.MontoDesembolso, c.MontoCredito) AS decimal(16, 2)) AS Monto,
            CAST(COALESCE(c.FechaDesembolso, c.FechaReg) AS datetime) AS FechaOp
        FROM CREDITO.Credito AS c
        WHERE c.OficinaId = @OficinaId
          AND c.FechaDesembolso IS NOT NULL
          AND c.FechaDesembolso >= @Desde
          AND c.FechaDesembolso < @Hasta

        UNION ALL

        SELECT
            CAST(N'COBRANZA' AS varchar(16)),
            mc.CreditoId,
            COALESCE(mc.PersonaId, c.PersonaId),
            cj.OficinaId,
            CAST(mc.ImportePago AS decimal(16, 2)),
            mc.FechaReg
        FROM CREDITO.MovimientoCaja AS mc
        INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = mc.CajaDiarioId
        INNER JOIN CREDITO.Caja AS cj ON cj.CajaId = cd.CajaId
        LEFT JOIN CREDITO.Credito AS c ON c.CreditoId = mc.CreditoId
        WHERE cj.OficinaId = @OficinaId
          AND mc.Estado = CAST(1 AS bit)
          AND mc.IndEntrada = CAST(1 AS bit)
          AND mc.Operacion = 'CUO'
          AND mc.ImportePago > 0
          AND mc.FechaReg >= @Desde
          AND mc.FechaReg < @Hasta
    ),
    Multiples AS (
        SELECT
            o.PersonaId,
            SUM(o.Monto) AS MontoMes
        FROM Ops AS o
        WHERE o.PersonaId IS NOT NULL
        GROUP BY o.PersonaId
    )
    SELECT
        o.Tipo,
        o.CreditoId,
        o.PersonaId,
        p.NumeroDocumento,
        p.NombreCompleto,
        o.Monto,
        o.FechaOp,
        CASE
            WHEN @TieneTc = 0 THEN CAST(0 AS bit)
            WHEN o.Monto >= @UmbralIndividualPen THEN CAST(1 AS bit)
            WHEN m.MontoMes >= @UmbralMultiplePen THEN CAST(1 AS bit)
            ELSE CAST(0 AS bit)
        END AS SuperaUmbral,
        CASE
            WHEN @TieneTc = 0 THEN N'Falta tipo de cambio SBS (promedio venta mes anterior)'
            WHEN o.Monto >= @UmbralIndividualPen THEN N'RO individual'
            WHEN m.MontoMes >= @UmbralMultiplePen THEN N'RO múltiples del mes'
            ELSE N'Bajo umbral'
        END AS MotivoRo,
        CASE WHEN NULLIF(LTRIM(RTRIM(p.NumeroDocumento)), N'') IS NULL
             THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS FaltaDocumento,
        CASE WHEN NULLIF(LTRIM(RTRIM(p.Direccion)), N'') IS NULL
             THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS FaltaDireccion,
        CASE WHEN cl.ActividadEconId IS NULL
             THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS FaltaOcupacion,
        @IndividualUsd AS IndividualEfectivoUsd,
        @MultipleUsd AS MultipleEfectivoUsd,
        @Tc AS TcVentaMesAnterior,
        @UmbralIndividualPen AS UmbralIndividualPen,
        @UmbralMultiplePen AS UmbralMultiplePen,
        @TieneTc AS TieneTipoCambio,
        m.MontoMes
    FROM Ops AS o
    LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = o.PersonaId
    LEFT JOIN MAESTRO.Cliente AS cl ON cl.PersonaId = o.PersonaId
    LEFT JOIN Multiples AS m ON m.PersonaId = o.PersonaId
    ORDER BY
        CASE
            WHEN @TieneTc = 1 AND o.Monto >= @UmbralIndividualPen THEN 0
            WHEN @TieneTc = 1 AND m.MontoMes >= @UmbralMultiplePen THEN 1
            ELSE 2
        END,
        o.Monto DESC,
        o.FechaOp DESC;
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_PlaRegistrarInusual]
    @OficinaId int,
    @PersonaId int,
    @CreditoId int = NULL,
    @Calificacion varchar(12),
    @Motivo varchar(500),
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @OficinaId IS NULL OR @OficinaId < 1
       OR @PersonaId IS NULL OR @PersonaId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
       OR NULLIF(LTRIM(RTRIM(@Motivo)), N'') IS NULL
    BEGIN
        RAISERROR(N'Datos de bitácora incompletos.', 16, 1);
        RETURN;
    END;

    SET @Calificacion = UPPER(LTRIM(RTRIM(@Calificacion)));
    IF @Calificacion NOT IN (N'INUSUAL', N'SOSPECHOSA', N'DESCARTADA')
    BEGIN
        RAISERROR(N'Calificación no válida.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM MAESTRO.Persona WHERE PersonaId = @PersonaId)
    BEGIN
        RAISERROR(N'Persona no encontrada.', 16, 1);
        RETURN;
    END;

    IF @CreditoId IS NOT NULL AND @CreditoId > 0
       AND NOT EXISTS (
            SELECT 1
            FROM CREDITO.Credito
            WHERE CreditoId = @CreditoId
              AND OficinaId = @OficinaId
              AND PersonaId = @PersonaId)
    BEGIN
        RAISERROR(N'Crédito no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    INSERT INTO CREDITO.PlaInusual (
        OficinaId, PersonaId, CreditoId, Calificacion, Motivo, UsuarioRegId, FechaReg)
    VALUES (
        @OficinaId,
        @PersonaId,
        NULLIF(@CreditoId, 0),
        @Calificacion,
        LEFT(LTRIM(RTRIM(@Motivo)), 500),
        @UsuarioId,
        dbo.ufnFecha());

    SELECT
        SCOPE_IDENTITY() AS PlaInusualId,
        @Calificacion AS Calificacion;
END
GO

CREATE OR ALTER PROC [CREDITO].[usp_PlaListarInusual]
    @OficinaId int,
    @Anio int,
    @Mes int
AS
BEGIN
    SET NOCOUNT ON;

    IF @OficinaId IS NULL OR @OficinaId < 1
       OR @Anio IS NULL OR @Anio < 2000
       OR @Mes IS NULL OR @Mes < 1 OR @Mes > 12
    BEGIN
        RAISERROR(N'Oficina y periodo inválidos.', 16, 1);
        RETURN;
    END;

    DECLARE @Desde datetime = DATEFROMPARTS(@Anio, @Mes, 1);
    DECLARE @Hasta datetime = DATEADD(MONTH, 1, @Desde);

    SELECT
        i.PlaInusualId,
        i.OficinaId,
        i.PersonaId,
        p.NumeroDocumento,
        p.NombreCompleto,
        i.CreditoId,
        i.Calificacion,
        i.Motivo,
        i.UsuarioRegId,
        u.NombreUsuario,
        i.FechaReg
    FROM CREDITO.PlaInusual AS i
    INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = i.PersonaId
    LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = i.UsuarioRegId
    WHERE i.OficinaId = @OficinaId
      AND i.FechaReg >= @Desde
      AND i.FechaReg < @Hasta
    ORDER BY i.FechaReg DESC;
END
GO
