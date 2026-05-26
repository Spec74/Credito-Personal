namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCreditoObservadoReadService
{
    /// <summary>
    /// Créditos con observación no vacía en estado PEN o DES (paridad <c>CreditoBL.ReporteCreditoObservado</c>).
    /// </summary>
    Task<IReadOnlyList<RptCreditoObservadoRowDto>> ListarAsync(
        int oficinaId,
        int? usuarioId,
        CancellationToken cancellationToken = default);
}
