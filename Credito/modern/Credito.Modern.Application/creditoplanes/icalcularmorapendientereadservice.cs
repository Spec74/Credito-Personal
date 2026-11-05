namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Referencia legacy <c>usp_CalcularMoraPendiente</c> (saldo×días del crédito).
/// No usar para cobro ni reportes operativos: la cifra a cobrar es la suma de
/// <c>ImporteMora</c> por cuota (<c>usp_CuotasPendientes</c> / plan de pagos).
/// </summary>
public interface ICalcularMoraPendienteReadService
{
    /// <summary>Ejecuta el proc y devuelve el primer valor escalar, o <c>null</c> si no hay filas.</summary>
    Task<decimal?> ObtenerAsync(int creditoId, CancellationToken cancellationToken = default);
}
