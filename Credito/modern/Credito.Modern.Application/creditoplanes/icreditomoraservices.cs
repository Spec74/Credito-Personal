namespace Credito.Modern.Application.CreditoPlanes;

public interface ICreditoMoraReadService
{
    Task<IReadOnlyList<CreditoMoraRowDto>> ListarPorCreditoAsync(
        int creditoId,
        CancellationToken cancellationToken = default);

    Task<decimal> ObtenerSaldoPostergadoAsync(
        int creditoId,
        CancellationToken cancellationToken = default);

    Task<bool> CreditoTieneMoraPostergadaHabilitadaAsync(
        int creditoId,
        CancellationToken cancellationToken = default);
}

public interface ICreditoMoraWriteService
{
    Task RegistrarPostergadaAsync(
        int creditoId,
        DateTime fechaVencimientoCuota,
        CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>usp_CreditoMora_Liquidar</c> (crea mov. MOR y recalcula caja).</summary>
    Task LiquidarAcumuladasAsync(
        int creditoId,
        int cajaDiarioId,
        int usuarioId,
        int tipoPagoId,
        CancellationToken cancellationToken = default);
}

public interface ICajaPagoMoraOrchestrator
{
    /// <summary>
    /// Tras un pago de cuotas: registra moras postergadas por cuota con atraso/mora
    /// y liquida acumuladas si corresponde (última cuota, producto IndMora).
    /// </summary>
    Task AplicarTrasPagoCuotasAsync(
        int creditoId,
        int cajaDiarioId,
        int usuarioId,
        int tipoPagoId,
        IReadOnlyList<int> planPagoIdsPagados,
        bool esUltimaCuota,
        CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>CajaDiarioBL.ProcesarPagoCompleto</c> (pago libre + liquidación mora).</summary>
    Task<PagoCajaResultResponse> ProcesarPagoCuotaConMoraAsync(
        int cajaDiarioId,
        int creditoId,
        decimal importeRecibido,
        int usuarioId,
        int tipoPagoId,
        string fechaPagoTransferencia,
        bool esUltimaCuota,
        CancellationToken cancellationToken = default);
}
