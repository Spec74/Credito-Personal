namespace Credito.Modern.Application.Almacenes;

public interface IListarSerieKardexReadService
{
    /// <summary>Ejecuta <c>ALMACEN.usp_ListarSerieKardex</c> y devuelve la primera fila (tipo string).</summary>
    Task<ListarSerieKardexResponse> ObtenerPrimeraFilaAsync(
        int movimientoDetalleId,
        bool indStock,
        CancellationToken cancellationToken = default);
}
