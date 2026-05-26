namespace Credito.Modern.Application.Almacenes;

public interface IRptConstanciaAlmacenReadService
{
    /// <summary>Paridad <c>ReporteController.ConstanciaAlmacen</c>.</summary>
    Task<RptConstanciaAlmacenDto?> ObtenerAsync(
        int movimientoId,
        CancellationToken cancellationToken = default);
}
