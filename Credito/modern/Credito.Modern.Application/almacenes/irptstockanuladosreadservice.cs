namespace Credito.Modern.Application.Almacenes;

public interface IRptStockAnuladosReadService
{
    Task<IReadOnlyList<RptStockAnuladoRowDto>> ListarAsync(CancellationToken cancellationToken = default);
}
