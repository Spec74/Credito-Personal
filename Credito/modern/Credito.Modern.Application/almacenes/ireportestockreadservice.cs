namespace Credito.Modern.Application.Almacenes;

public interface IReporteStockReadService
{
    /// <summary>Ejecuta <c>ALMACEN.usp_ReporteStock</c> para la oficina indicada.</summary>
    Task<List<ReporteStockRowDto>> ListarPorOficinaAsync(int oficinaId, CancellationToken cancellationToken = default);
}
