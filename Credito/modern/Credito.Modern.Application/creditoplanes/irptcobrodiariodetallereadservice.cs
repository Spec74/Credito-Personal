namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCobroDiarioDetalleReadService
{
    Task<List<RptCobroDiarioDetalleRowDto>> ListarAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>CreditoBL.ReporteCobranzaGestor</c> / pantalla MVC <c>CobranzaPagos</c>.</summary>
    Task<List<RptCobroDiarioDetalleRowDto>> ListarCobranzaAsync(
        int? usuarioId,
        int? oficinaId,
        CancellationToken cancellationToken = default);
}
