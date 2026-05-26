namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCreditoVencidoReadService
{
    /// <summary>
    /// Ejecuta <c>CREDITO.usp_RptCreditoVencido</c>. Cada indicador opcional debe ser <c>null</c>, <c>S</c> o <c>N</c> (un carácter, sin distinguir mayúsculas).
    /// </summary>
    Task<IReadOnlyList<RptCreditoVencidoRowDto>> ListarAsync(
        string? vencidoMenor60,
        string? vencidoMayor60,
        string? vencidoIrrecuperable,
        CancellationToken cancellationToken = default);
}
