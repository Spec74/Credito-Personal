namespace Credito.Modern.Application.Ventas;

public interface IVentaRapidaCajaDiarioReadService
{
    /// <summary>
    /// Caja diario no cerrada asignada al usuario en la oficina, o <c>null</c>.
    /// </summary>
    Task<CajaDiarioVentaRapidaDto?> ObtenerAbiertaPorUsuarioAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}
