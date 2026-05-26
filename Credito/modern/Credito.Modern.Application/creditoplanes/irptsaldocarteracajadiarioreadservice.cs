namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptSaldoCarteraCajaDiarioReadService
{
    /// <summary>Ejecuta <c>CREDITO.usp_RptSaldoCarteraCajaDiario</c> (<c>UsuarioId</c>, <c>OficinaId</c>, <c>AnioIni</c>, <c>MesIni</c>, <c>AnioFin</c>, <c>MesFin</c>).</summary>
    Task<IReadOnlyList<RptSaldoCarteraCajaDiarioRowDto>> ListarAsync(
        int? usuarioId,
        int oficinaId,
        int anioIni,
        int mesIni,
        int anioFin,
        int mesFin,
        CancellationToken cancellationToken = default);
}
