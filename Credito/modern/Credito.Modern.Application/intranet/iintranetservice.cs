namespace Credito.Modern.Application.Intranet;

public interface IIntranetService
{
    Task<IReadOnlyList<ComunicadoRowDto>> ListarComunicadosAsync(
        int oficinaId, int usuarioId, bool incluirVencidos, CancellationToken ct = default);

    Task<(ComunicadoRowDto? Cab, IReadOnlyList<ComunicadoAdjuntoDto> Adjuntos)> ObtenerComunicadoAsync(
        int comunicadoId, int oficinaId, int usuarioId, CancellationToken ct = default);

    Task<int> GuardarComunicadoAsync(
        GuardarComunicadoRequest request, int oficinaId, int usuarioId, CancellationToken ct = default);

    Task BajaComunicadoAsync(int comunicadoId, int usuarioId, CancellationToken ct = default);

    Task MarcarLeidoAsync(int comunicadoId, int usuarioId, CancellationToken ct = default);

    Task<int> SubirAdjuntoAsync(
        int comunicadoId, string nombre, string contentType, byte[] contenido, CancellationToken ct = default);

    Task<ComunicadoAdjuntoContenidoDto?> DescargarAdjuntoAsync(
        int adjuntoId, int oficinaId, CancellationToken ct = default);

    Task<IReadOnlyList<AgendaEventoDto>> ListarAgendaAsync(
        int oficinaId, int usuarioId, DateTime desde, DateTime hasta, CancellationToken ct = default);

    Task<int> GuardarAgendaAsync(
        GuardarAgendaRequest request, int oficinaId, int usuarioId, CancellationToken ct = default);

    Task BajaAgendaAsync(int eventoId, int usuarioId, CancellationToken ct = default);

    Task DescartarAgendaAsync(int eventoId, int usuarioId, CancellationToken ct = default);

    Task<IReadOnlyList<BandejaItemDto>> BandejaAsync(
        int oficinaId, int usuarioId, CancellationToken ct = default);
}
