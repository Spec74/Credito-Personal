namespace Credito.Modern.Application.Ventas;

public interface ICodigoBarrasLstReadService
{
    /// <summary>Ejecuta <c>VENTAS.usp_CodigoBarras_Lst</c> con <paramref name="movimientoId"/>.</summary>
    Task<List<CodigoBarrasLstRowDto>> ListarPorMovimientoAsync(int movimientoId, CancellationToken cancellationToken = default);
}
