namespace Credito.Modern.Application.Ventas;

public interface IRptListaPrecioGeneralReadService
{
    Task<IReadOnlyList<RptListaPrecioGeneralRowDto>> ListarAsync(
        int? marcaId,
        bool indDescuento,
        bool indPuntos,
        CancellationToken cancellationToken = default);
}
