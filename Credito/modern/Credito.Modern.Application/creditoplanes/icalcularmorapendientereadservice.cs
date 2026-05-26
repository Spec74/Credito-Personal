namespace Credito.Modern.Application.CreditoPlanes;

public interface ICalcularMoraPendienteReadService
{
    /// <summary>Ejecuta el proc y devuelve el primer valor escalar, o <c>null</c> si no hay filas.</summary>
    Task<decimal?> ObtenerAsync(int creditoId, CancellationToken cancellationToken = default);
}
