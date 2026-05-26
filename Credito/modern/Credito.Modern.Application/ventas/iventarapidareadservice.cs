namespace Credito.Modern.Application.Ventas;

public interface IVentaRapidaReadService
{
    Task<ArticuloVentaRapidaDto?> ObtenerPorCodigoAsync(
        string codArticulo,
        CancellationToken cancellationToken = default);
}
