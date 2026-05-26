namespace Credito.Modern.Application.Almacenes;

public interface IGenerarKardexReadService
{
    /// <summary>Ejecuta <c>ALMACEN.usp_GenerarKardex</c> (<c>ArticuloId</c>, <c>AlmacenId</c>).</summary>
    Task<IReadOnlyList<GenerarKardexRowDto>> ListarAsync(
        int articuloId,
        int almacenId,
        CancellationToken cancellationToken = default);
}
