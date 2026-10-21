-- Comunicados (avisos/reglamentos) + agenda. Adjuntos en SQL (App Service sin disco durable).

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'MAESTRO.Comunicado', N'U') IS NULL
BEGIN
    CREATE TABLE MAESTRO.Comunicado (
        ComunicadoId    int IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_Comunicado PRIMARY KEY,
        Titulo          varchar(160) NOT NULL,
        Cuerpo          varchar(4000) NOT NULL,
        Alcance         varchar(12) NOT NULL,
        OficinaId       int NULL,
        FechaIni        datetime NOT NULL,
        FechaFin        datetime NULL,
        Estado          bit NOT NULL CONSTRAINT DF_Comunicado_Estado DEFAULT (1),
        UsuarioRegId    int NOT NULL,
        FechaReg        datetime NOT NULL,
        CONSTRAINT CK_Comunicado_Alcance CHECK (Alcance IN (N'RED', N'OFICINA'))
    );
    CREATE INDEX IX_Comunicado_Vigencia ON MAESTRO.Comunicado (Estado, FechaIni, FechaFin);
END
GO

IF OBJECT_ID(N'MAESTRO.ComunicadoAdjunto', N'U') IS NULL
BEGIN
    CREATE TABLE MAESTRO.ComunicadoAdjunto (
        AdjuntoId       int IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_ComunicadoAdjunto PRIMARY KEY,
        ComunicadoId    int NOT NULL,
        NombreOriginal  varchar(200) NOT NULL,
        ContentType     varchar(80) NOT NULL,
        TamanoBytes     int NOT NULL,
        Contenido       varbinary(max) NOT NULL,
        CONSTRAINT FK_ComunicadoAdjunto_Comunicado
            FOREIGN KEY (ComunicadoId) REFERENCES MAESTRO.Comunicado (ComunicadoId)
    );
END
GO

IF OBJECT_ID(N'MAESTRO.ComunicadoLectura', N'U') IS NULL
BEGIN
    CREATE TABLE MAESTRO.ComunicadoLectura (
        ComunicadoId    int NOT NULL,
        UsuarioId       int NOT NULL,
        FechaLectura    datetime NOT NULL,
        CONSTRAINT PK_ComunicadoLectura PRIMARY KEY (ComunicadoId, UsuarioId)
    );
END
GO

IF OBJECT_ID(N'MAESTRO.AgendaEvento', N'U') IS NULL
BEGIN
    CREATE TABLE MAESTRO.AgendaEvento (
        EventoId            int IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_AgendaEvento PRIMARY KEY,
        Titulo              varchar(160) NOT NULL,
        Notas               varchar(1000) NULL,
        FechaIni            datetime NOT NULL,
        FechaFin            datetime NULL,
        TodoElDia           bit NOT NULL CONSTRAINT DF_Agenda_TodoElDia DEFAULT (0),
        RecordatorioMinutos int NULL,
        Alcance             varchar(12) NOT NULL,
        OficinaId           int NULL,
        UsuarioId           int NOT NULL,
        Estado              bit NOT NULL CONSTRAINT DF_Agenda_Estado DEFAULT (1),
        UsuarioRegId        int NOT NULL,
        FechaReg            datetime NOT NULL,
        CONSTRAINT CK_Agenda_Alcance CHECK (Alcance IN (N'PERSONAL', N'OFICINA'))
    );
    CREATE INDEX IX_Agenda_Usuario ON MAESTRO.AgendaEvento (UsuarioId, FechaIni);
    CREATE INDEX IX_Agenda_Oficina ON MAESTRO.AgendaEvento (OficinaId, FechaIni);
END
GO

IF OBJECT_ID(N'MAESTRO.AgendaDescarte', N'U') IS NULL
BEGIN
    CREATE TABLE MAESTRO.AgendaDescarte (
        EventoId    int NOT NULL,
        UsuarioId   int NOT NULL,
        FechaReg    datetime NOT NULL,
        CONSTRAINT PK_AgendaDescarte PRIMARY KEY (EventoId, UsuarioId)
    );
END
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

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoMarcarLeido]
    @ComunicadoId int,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM MAESTRO.Comunicado
        WHERE ComunicadoId = @ComunicadoId AND Estado = 1)
    BEGIN
        RAISERROR(N'Aviso no encontrado.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM MAESTRO.ComunicadoLectura
        WHERE ComunicadoId = @ComunicadoId AND UsuarioId = @UsuarioId)
    BEGIN
        INSERT INTO MAESTRO.ComunicadoLectura (ComunicadoId, UsuarioId, FechaLectura)
        VALUES (@ComunicadoId, @UsuarioId, dbo.ufnFecha());
    END;
    SELECT @ComunicadoId AS ComunicadoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_ComunicadoAdjuntoIns]
    @ComunicadoId int,
    @NombreOriginal varchar(200),
    @ContentType varchar(80),
    @Contenido varbinary(max)
AS
BEGIN
    SET NOCOUNT ON;
    IF @Contenido IS NULL OR DATALENGTH(@Contenido) < 1
    BEGIN
        RAISERROR(N'Archivo vacío.', 16, 1);
        RETURN;
    END;
    INSERT INTO MAESTRO.ComunicadoAdjunto (
        ComunicadoId, NombreOriginal, ContentType, TamanoBytes, Contenido)
    VALUES (
        @ComunicadoId,
        LEFT(@NombreOriginal, 200),
        LEFT(@ContentType, 80),
        DATALENGTH(@Contenido),
        @Contenido);
    SELECT SCOPE_IDENTITY() AS AdjuntoId, DATALENGTH(@Contenido) AS TamanoBytes;
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

CREATE OR ALTER PROC [MAESTRO].[usp_AgendaListar]
    @OficinaId int,
    @UsuarioId int,
    @Desde datetime,
    @Hasta datetime
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.EventoId,
        e.Titulo,
        e.Notas,
        e.FechaIni,
        e.FechaFin,
        e.TodoElDia,
        e.RecordatorioMinutos,
        e.Alcance,
        e.OficinaId,
        e.UsuarioId,
        CASE WHEN e.UsuarioId = @UsuarioId THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS EsMio
    FROM MAESTRO.AgendaEvento AS e
    WHERE e.Estado = 1
      AND e.FechaIni < @Hasta
      AND COALESCE(e.FechaFin, e.FechaIni) >= @Desde
      AND (
            (e.Alcance = N'PERSONAL' AND e.UsuarioId = @UsuarioId)
            OR (e.Alcance = N'OFICINA' AND e.OficinaId = @OficinaId)
          )
    ORDER BY e.FechaIni;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_AgendaGuardar]
    @EventoId int = NULL,
    @Titulo varchar(160),
    @Notas varchar(1000) = NULL,
    @FechaIni datetime,
    @FechaFin datetime = NULL,
    @TodoElDia bit,
    @RecordatorioMinutos int = NULL,
    @Alcance varchar(12),
    @OficinaId int,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;
    IF NULLIF(LTRIM(RTRIM(@Titulo)), N'') IS NULL OR @FechaIni IS NULL
    BEGIN
        RAISERROR(N'Datos de agenda incompletos.', 16, 1);
        RETURN;
    END;
    SET @Alcance = UPPER(LTRIM(RTRIM(@Alcance)));
    IF @Alcance NOT IN (N'PERSONAL', N'OFICINA')
    BEGIN
        RAISERROR(N'Alcance de agenda no válido.', 16, 1);
        RETURN;
    END;

    IF @EventoId IS NULL OR @EventoId < 1
    BEGIN
        INSERT INTO MAESTRO.AgendaEvento (
            Titulo, Notas, FechaIni, FechaFin, TodoElDia, RecordatorioMinutos,
            Alcance, OficinaId, UsuarioId, Estado, UsuarioRegId, FechaReg)
        VALUES (
            LEFT(LTRIM(RTRIM(@Titulo)), 160),
            NULLIF(LTRIM(RTRIM(@Notas)), N''),
            @FechaIni,
            @FechaFin,
            @TodoElDia,
            @RecordatorioMinutos,
            @Alcance,
            CASE WHEN @Alcance = N'OFICINA' THEN @OficinaId ELSE NULL END,
            @UsuarioId,
            1,
            @UsuarioId,
            dbo.ufnFecha());
        SET @EventoId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE MAESTRO.AgendaEvento
        SET Titulo = LEFT(LTRIM(RTRIM(@Titulo)), 160),
            Notas = NULLIF(LTRIM(RTRIM(@Notas)), N''),
            FechaIni = @FechaIni,
            FechaFin = @FechaFin,
            TodoElDia = @TodoElDia,
            RecordatorioMinutos = @RecordatorioMinutos,
            Alcance = @Alcance,
            OficinaId = CASE WHEN @Alcance = N'OFICINA' THEN @OficinaId ELSE NULL END
        WHERE EventoId = @EventoId
          AND Estado = 1
          AND UsuarioId = @UsuarioId;
        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR(N'Evento no encontrado o no es suyo.', 16, 1);
            RETURN;
        END
    END;
    SELECT @EventoId AS EventoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_AgendaBaja]
    @EventoId int,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MAESTRO.AgendaEvento
    SET Estado = 0
    WHERE EventoId = @EventoId
      AND Estado = 1
      AND UsuarioId = @UsuarioId;
    SELECT @@ROWCOUNT AS Filas;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_AgendaDescartar]
    @EventoId int,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM MAESTRO.AgendaDescarte
        WHERE EventoId = @EventoId AND UsuarioId = @UsuarioId)
    BEGIN
        INSERT INTO MAESTRO.AgendaDescarte (EventoId, UsuarioId, FechaReg)
        VALUES (@EventoId, @UsuarioId, dbo.ufnFecha());
    END;
    SELECT @EventoId AS EventoId;
END
GO

CREATE OR ALTER PROC [MAESTRO].[usp_BandejaNotificaciones]
    @OficinaId int,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ahora datetime = dbo.ufnFecha();

    SELECT
        CAST(N'COMUNICADO' AS varchar(16)) AS Tipo,
        c.ComunicadoId AS Id,
        c.Titulo,
        c.FechaReg AS Fecha,
        CAST(0 AS bit) AS PermanenteNo
    FROM MAESTRO.Comunicado AS c
    WHERE c.Estado = 1
      AND c.FechaIni <= @Ahora
      AND (c.FechaFin IS NULL OR c.FechaFin >= @Ahora)
      AND (c.Alcance = N'RED' OR c.OficinaId = @OficinaId)
      AND NOT EXISTS (
            SELECT 1 FROM MAESTRO.ComunicadoLectura l
            WHERE l.ComunicadoId = c.ComunicadoId AND l.UsuarioId = @UsuarioId)

    UNION ALL

    SELECT
        CAST(N'AGENDA' AS varchar(16)),
        e.EventoId,
        e.Titulo,
        e.FechaIni,
        CAST(0 AS bit)
    FROM MAESTRO.AgendaEvento AS e
    WHERE e.Estado = 1
      AND e.RecordatorioMinutos IS NOT NULL
      AND DATEADD(MINUTE, -e.RecordatorioMinutos, e.FechaIni) <= @Ahora
      AND e.FechaIni >= DATEADD(DAY, -1, @Ahora)
      AND (
            (e.Alcance = N'PERSONAL' AND e.UsuarioId = @UsuarioId)
            OR (e.Alcance = N'OFICINA' AND e.OficinaId = @OficinaId)
          )
      AND NOT EXISTS (
            SELECT 1 FROM MAESTRO.AgendaDescarte d
            WHERE d.EventoId = e.EventoId AND d.UsuarioId = @UsuarioId)
    ORDER BY Fecha DESC;
END
GO
