namespace Credito.Modern.Application.Almacenes;

public interface IExisteSerieArticuloReadService
{
    /// <summary>Ejecuta <c>ALMACEN.usp_ExisteSerieArticulo</c> y devuelve la primera fila (tipo string).</summary>
    Task<ExisteSerieArticuloResponse> ValidarAsync(
        string listaSerie,
        int? cantidad,
        bool indCorrelativo,
        CancellationToken cancellationToken = default);
}
