namespace Credito.Modern.Application.Documentos;

public interface ITipoDocumentoReadService
{
    /// <summary>
    /// Tipos de documento activos. Si <paramref name="soloParaVenta"/> es true, solo <c>IndVenta = 1</c>
    /// (común en pantallas de venta del legado).
    /// </summary>
    Task<List<TipoDocumentoListItemDto>> GetActivosAsync(bool soloParaVenta, CancellationToken cancellationToken = default);

    /// <summary>Paridad EntradaController: IndAlmacen y sin IndAlmacenMov.</summary>
    Task<List<TipoDocumentoListItemDto>> GetActivosAlmacenMovAsync(
        CancellationToken cancellationToken = default);
}
