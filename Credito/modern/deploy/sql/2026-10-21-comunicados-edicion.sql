-- Completa avisos: destino una sede o toda la red, editar y eliminar.
-- Idempotente (CREATE OR ALTER). Aplicar también en Azure si ya corrió comunicados-agenda.sql.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoListar]
    @OficinaId int,
    @UsuarioId int,
    @IncluirVencidos bit = 0,
    @Gestion bit = 0,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ahora datetime = dbo.ufnFecha();

    SELECT
        c.ComunicadoId,
        c.Titulo,
        c.Cuerpo,
        c.Alcance,
        c.OficinaId,
        o.Denominacion AS OficinaNombre,
        c.FechaIni,
        c.FechaFin,
        CASE WHEN c.FechaFin IS NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Permanente,
        CASE WHEN c.FechaIni <= @Ahora
              AND (c.FechaFin IS NULL OR c.FechaFin >= @Ahora)
             THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Vigente,
        CASE WHEN l.UsuarioId IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS Leido,
        (SELECT COUNT(*) FROM MAESTRO.ComunicadoAdjunto a WHERE a.ComunicadoId = c.ComunicadoId) AS Adjuntos,
        c.UsuarioRegId,
        u.NombreUsuario,
        c.FechaReg
    FROM MAESTRO.Comunicado AS c
    LEFT JOIN MAESTRO.ComunicadoLectura AS l
        ON l.ComunicadoId = c.ComunicadoId AND l.UsuarioId = @UsuarioId
    LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
    LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = c.OficinaId
    WHERE c.Estado = CAST(1 AS bit)
      AND (
            (@Gestion = 1 AND @EsAdmin = 1)
            OR c.Alcance = N'RED'
            OR c.OficinaId = @OficinaId
          )
      AND (
            @IncluirVencidos = 1
            OR (c.FechaIni <= @Ahora AND (c.FechaFin IS NULL OR c.FechaFin >= @Ahora))
          )
    ORDER BY
        CASE WHEN l.UsuarioId IS NULL THEN 0 ELSE 1 END,
        CASE WHEN c.FechaFin IS NULL THEN 0 ELSE 1 END,
        c.FechaReg DESC;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoObtener]
    @ComunicadoId int,
    @OficinaId int,
    @UsuarioId int,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ahora datetime = dbo.ufnFecha();

    SELECT
        c.ComunicadoId,
        c.Titulo,
        c.Cuerpo,
        c.Alcance,
        c.OficinaId,
        o.Denominacion AS OficinaNombre,
        c.FechaIni,
        c.FechaFin,
        CASE WHEN c.FechaFin IS NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Permanente,
        CASE WHEN c.FechaIni <= @Ahora
              AND (c.FechaFin IS NULL OR c.FechaFin >= @Ahora)
             THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Vigente,
        CASE WHEN l.UsuarioId IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS Leido,
        (SELECT COUNT(*) FROM MAESTRO.ComunicadoAdjunto a WHERE a.ComunicadoId = c.ComunicadoId) AS Adjuntos,
        c.UsuarioRegId,
        u.NombreUsuario,
        c.FechaReg
    FROM MAESTRO.Comunicado AS c
    LEFT JOIN MAESTRO.ComunicadoLectura AS l
        ON l.ComunicadoId = c.ComunicadoId AND l.UsuarioId = @UsuarioId
    LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
    LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = c.OficinaId
    WHERE c.ComunicadoId = @ComunicadoId
      AND c.Estado = CAST(1 AS bit)
      AND (
            @EsAdmin = 1
            OR c.Alcance = N'RED'
            OR c.OficinaId = @OficinaId
          );

    SELECT
        a.AdjuntoId,
        a.ComunicadoId,
        a.NombreOriginal,
        a.ContentType,
        a.TamanoBytes
    FROM MAESTRO.ComunicadoAdjunto AS a
    INNER JOIN MAESTRO.Comunicado AS c ON c.ComunicadoId = a.ComunicadoId
    WHERE a.ComunicadoId = @ComunicadoId
      AND c.Estado = CAST(1 AS bit)
      AND (
            @EsAdmin = 1
            OR c.Alcance = N'RED'
            OR c.OficinaId = @OficinaId
          );
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoGuardar]
    @ComunicadoId int = NULL,
    @Titulo varchar(160),
    @Cuerpo varchar(4000),
    @Alcance varchar(12),
    @OficinaId int,
    @FechaIni datetime,
    @FechaFin datetime = NULL,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;

    IF NULLIF(LTRIM(RTRIM(@Titulo)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Cuerpo)), N'') IS NULL
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos del aviso incompletos.', 16, 1);
        RETURN;
    END;

    SET @Alcance = UPPER(LTRIM(RTRIM(@Alcance)));
    IF @Alcance NOT IN (N'RED', N'OFICINA')
    BEGIN
        RAISERROR(N'Alcance no válido.', 16, 1);
        RETURN;
    END;

    IF @Alcance = N'OFICINA' AND (@OficinaId IS NULL OR @OficinaId < 1)
    BEGIN
        RAISERROR(N'Elija la sede donde será visible el aviso.', 16, 1);
        RETURN;
    END;

    IF @Alcance = N'OFICINA'
       AND NOT EXISTS (SELECT 1 FROM MAESTRO.Oficina WHERE OficinaId = @OficinaId AND Estado = 1)
    BEGIN
        RAISERROR(N'La sede no existe o está inactiva.', 16, 1);
        RETURN;
    END;

    IF @FechaFin IS NOT NULL AND @FechaFin < @FechaIni
    BEGIN
        RAISERROR(N'La fecha fin no puede ser anterior al inicio.', 16, 1);
        RETURN;
    END;

    IF @ComunicadoId IS NULL OR @ComunicadoId < 1
    BEGIN
        INSERT INTO MAESTRO.Comunicado (
            Titulo, Cuerpo, Alcance, OficinaId, FechaIni, FechaFin, Estado, UsuarioRegId, FechaReg)
        VALUES (
            LEFT(LTRIM(RTRIM(@Titulo)), 160),
            LEFT(LTRIM(RTRIM(@Cuerpo)), 4000),
            @Alcance,
            CASE WHEN @Alcance = N'RED' THEN NULL ELSE @OficinaId END,
            @FechaIni,
            @FechaFin,
            1,
            @UsuarioId,
            dbo.ufnFecha());
        SET @ComunicadoId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE MAESTRO.Comunicado
        SET Titulo = LEFT(LTRIM(RTRIM(@Titulo)), 160),
            Cuerpo = LEFT(LTRIM(RTRIM(@Cuerpo)), 4000),
            Alcance = @Alcance,
            OficinaId = CASE WHEN @Alcance = N'RED' THEN NULL ELSE @OficinaId END,
            FechaIni = @FechaIni,
            FechaFin = @FechaFin
        WHERE ComunicadoId = @ComunicadoId
          AND Estado = CAST(1 AS bit);
        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR(N'Aviso no encontrado.', 16, 1);
            RETURN;
        END
    END;

    SELECT @ComunicadoId AS ComunicadoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoBaja]
    @ComunicadoId int,
    @UsuarioId int,
    @OficinaId int,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MAESTRO.Comunicado
    SET Estado = 0
    WHERE ComunicadoId = @ComunicadoId
      AND Estado = 1
      AND (
            @EsAdmin = 1
            OR (Alcance = N'OFICINA' AND OficinaId = @OficinaId)
          );
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'No puede retirar este aviso.', 16, 1);
        RETURN;
    END;
    SELECT @ComunicadoId AS ComunicadoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoAdjuntoGet]
    @AdjuntoId int,
    @OficinaId int,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.AdjuntoId,
        a.NombreOriginal,
        a.ContentType,
        a.TamanoBytes,
        a.Contenido
    FROM MAESTRO.ComunicadoAdjunto AS a
    INNER JOIN MAESTRO.Comunicado AS c ON c.ComunicadoId = a.ComunicadoId
    WHERE a.AdjuntoId = @AdjuntoId
      AND c.Estado = 1
      AND (
            @EsAdmin = 1
            OR c.Alcance = N'RED'
            OR c.OficinaId = @OficinaId
          );
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoAdjuntoDel]
    @AdjuntoId int,
    @OficinaId int,
    @EsAdmin bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    DELETE a
    FROM MAESTRO.ComunicadoAdjunto AS a
    INNER JOIN MAESTRO.Comunicado AS c ON c.ComunicadoId = a.ComunicadoId
    WHERE a.AdjuntoId = @AdjuntoId
      AND c.Estado = 1
      AND (
            @EsAdmin = 1
            OR (c.Alcance = N'OFICINA' AND c.OficinaId = @OficinaId)
          );
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'No puede quitar este archivo.', 16, 1);
        RETURN;
    END;
    SELECT @AdjuntoId AS AdjuntoId;
END
GO
