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
