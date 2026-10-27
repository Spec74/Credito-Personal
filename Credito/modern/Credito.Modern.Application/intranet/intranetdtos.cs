namespace Credito.Modern.Application.Intranet;

public sealed record ComunicadoRowDto(
    int ComunicadoId,
    string Titulo,
    string Cuerpo,
    string Alcance,
    int? OficinaId,
    string? OficinaNombre,
    DateTime FechaIni,
    DateTime? FechaFin,
    bool Permanente,
    bool Vigente,
    bool Leido,
    int Adjuntos,
    int UsuarioRegId,
    string? NombreUsuario,
    DateTime FechaReg);

public sealed record ComunicadoAdjuntoDto(
    int AdjuntoId,
    int ComunicadoId,
    string NombreOriginal,
    string ContentType,
    int TamanoBytes);

public sealed record GuardarComunicadoRequest(
    int? ComunicadoId,
    string Titulo,
    string Cuerpo,
    string Alcance,
    DateTime FechaIni,
    DateTime? FechaFin,
    int? OficinaDestinoId = null);

public sealed record IdResponse(int Id);

public sealed record AgendaEventoDto(
    int EventoId,
    string Titulo,
    string? Notas,
    DateTime FechaIni,
    DateTime? FechaFin,
    bool TodoElDia,
    int? RecordatorioMinutos,
    string Alcance,
    int? OficinaId,
    int UsuarioId,
    bool EsMio);

public sealed record GuardarAgendaRequest(
    int? EventoId,
    string Titulo,
    string? Notas,
    DateTime FechaIni,
    DateTime? FechaFin,
    bool TodoElDia,
    int? RecordatorioMinutos,
    string Alcance);

public sealed record BandejaItemDto(
    string Tipo,
    int Id,
    string Titulo,
    DateTime Fecha,
    bool PermanenteNo);

public sealed record ComunicadoAdjuntoContenidoDto(
    int AdjuntoId,
    string NombreOriginal,
    string ContentType,
    int TamanoBytes,
    byte[] Contenido);
