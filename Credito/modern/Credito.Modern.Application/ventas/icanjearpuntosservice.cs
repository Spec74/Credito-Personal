namespace Credito.Modern.Application.Ventas;

public interface ICanjearPuntosService
{
    Task<TarjetaPuntoDto?> ObtenerTarjetaAsync(
        int personaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArticuloCanjeListItemDto>> ListarArticulosCanjeablesAsync(
        int personaId,
        CancellationToken cancellationToken = default);

    Task<string> CanjearAsync(
        int personaId,
        string numeroSerie,
        CancellationToken cancellationToken = default);
}
