namespace Credito.Modern.Application.CreditoTareas;

public interface ITareasReadService
{
    /// <param name="estado">PEN, COM o vacío para todas.</param>
    /// <param name="alcanceRed">Admin de red (ADMIN + Central): puede ver todas las sedes.</param>
    Task<IReadOnlyList<TareaListItemDto>> ListarPorUsuarioAsync(
        int usuarioId,
        int oficinaId,
        string? estado,
        bool alcanceRed = false,
        CancellationToken cancellationToken = default);

    Task<TareaDetalleDto?> ObtenerDetalleAsync(
        int tareaId,
        int usuarioId,
        int oficinaId,
        bool alcanceRed = false,
        CancellationToken cancellationToken = default);

    /// <summary>Créditos de la oficina (o red si <paramref name="alcanceRed"/>).</summary>
    Task<IReadOnlyList<CreditoTareaBuscarDto>> BuscarCreditosAsync(
        string term,
        int oficinaId,
        bool alcanceRed = false,
        CancellationToken cancellationToken = default);

    Task<bool> PuedeEditarTareaAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<bool> UsuarioPuedeAccederTareaAsync(
        int tareaId,
        int usuarioId,
        int oficinaId,
        bool alcanceRed = false,
        CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>TareaBL.ListarTareasParaReporte</c> (estado default PEN).</summary>
    Task<IReadOnlyList<TareaReporteRowDto>> ListarParaReporteAsync(
        int usuarioId,
        int oficinaId,
        string? estado,
        bool alcanceRed = false,
        CancellationToken cancellationToken = default);
}
