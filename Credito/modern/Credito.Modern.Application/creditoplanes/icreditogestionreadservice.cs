namespace Credito.Modern.Application.CreditoPlanes;

public interface ICreditoGestionReadService
{
    Task<PersonaCreditoFichaDto?> ObtenerPersonaCreditoFichaAsync(
        int oficinaId,
        int personaId,
        CancellationToken cancellationToken = default);

    Task<CreditoContextoDto?> ObtenerContextoAsync(
        int creditoId,
        CancellationToken cancellationToken = default);

    Task<SolicitudCreditoDetalleDto?> ObtenerSolicitudCreditoAsync(
        int solicitudCreditoId,
        CancellationToken cancellationToken = default);

    Task<CreditoPrendaDto?> ObtenerPrendaAsync(
        int creditoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CargoCreditoRowDto>> ListarCargosAsync(
        int creditoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CreditoEvidenciaDto>> ListarEvidenciasAsync(
        int creditoId,
        CancellationToken cancellationToken = default);

    Task<CreditoGrillaPersonaPageDto> ListarCreditosGrillaPersonaAsync(
        int oficinaId,
        int personaId,
        bool grupoActivo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(int CreditoId, string FileName)?> ObtenerEvidenciaArchivoAsync(
        int creditoImagenId,
        CancellationToken cancellationToken = default);
}
