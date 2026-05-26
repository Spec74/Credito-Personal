namespace Credito.Modern.Application.CreditoTareas;

public interface ITareasReadService
{
    /// <param name="estado">PEN, COM o vacío para todas.</param>
    Task<IReadOnlyList<TareaListItemDto>> ListarPorUsuarioAsync(
        int usuarioId,
        int oficinaId,
        string? estado,
        CancellationToken cancellationToken = default);

    Task<TareaDetalleDto?> ObtenerDetalleAsync(
        int tareaId,
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CreditoTareaBuscarDto>> BuscarCreditosAsync(
        string term,
        CancellationToken cancellationToken = default);

    Task<bool> PuedeEditarTareaAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<bool> UsuarioPuedeAccederTareaAsync(
        int tareaId,
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>TareaBL.ListarTareasParaReporte</c> (estado default PEN).</summary>
    Task<IReadOnlyList<TareaReporteRowDto>> ListarParaReporteAsync(
        int usuarioId,
        int oficinaId,
        string? estado,
        CancellationToken cancellationToken = default);
}
