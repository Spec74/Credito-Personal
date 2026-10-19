namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Paridad <c>GastosAdmBL.CalcularGastosAdm</c> (tabla <c>CREDITO.GastosAdm</c>).</summary>
public interface IGastosAdmReadService
{
    /// <param name="importe">Monto del crédito.</param>
    /// <param name="incluyeCentralRiesgo">
    /// Si false, excluye filas con <c>Ind = 1</c> (cargo de central de riesgo).
    /// No resta del desembolso: con tipo ADE el cliente recibe el monto completo.
    /// </param>
    Task<decimal> CalcularAsync(
        decimal importe,
        bool incluyeCentralRiesgo,
        CancellationToken cancellationToken = default);
}
