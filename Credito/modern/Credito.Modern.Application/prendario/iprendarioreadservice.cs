namespace Credito.Modern.Application.Prendario;

public interface IPrendarioReadService
{
    Task<PrendarioResumenDto> ObtenerResumenAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<PrendarioListaPageDto> ListarAsync(
        int oficinaId,
        string? buscar,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PrendarioDocumentoConsulta<PrendarioContratoDto>> ObtenerContratoAsync(
        int oficinaId,
        int creditoId,
        CancellationToken cancellationToken = default);

    Task<PrendarioDocumentoConsulta<PrendarioActaDto>> ObtenerActaAsync(
        int oficinaId,
        int creditoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrendarioAvisoVencimientoDto>> ListarAvisosVencimientoAsync(
        int? oficinaId,
        int diasAntes,
        CancellationToken cancellationToken = default);

    Task<bool> MarcarNotificadoWhatsAppAsync(
        int oficinaId,
        int creditoId,
        CancellationToken cancellationToken = default);
}
