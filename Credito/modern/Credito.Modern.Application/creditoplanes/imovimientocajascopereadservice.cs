namespace Credito.Modern.Application.CreditoPlanes;

public interface IMovimientoCajaScopeReadService
{
    Task<MovimientoCajaScopeDto?> GetScopeAsync(int movimientoCajaId, CancellationToken cancellationToken = default);

    /// <summary>Paridad MVC: INI con cuotas PAG en el crédito de la cuenta por cobrar del movimiento.</summary>
    Task<bool> RequiereConfirmacionAnularAsync(int movimientoCajaId, CancellationToken cancellationToken = default);
}
