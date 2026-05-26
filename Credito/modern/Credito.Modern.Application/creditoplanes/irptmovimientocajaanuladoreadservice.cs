namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptMovimientoCajaAnuladoReadService
{
    /// <summary>Ejecuta <c>CREDITO.usp_RptMovimientoCajaAnulado</c> (<c>FechaIni</c>, <c>FechaFin</c>).</summary>
    Task<IReadOnlyList<RptMovimientoCajaAnuladoRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        CancellationToken cancellationToken = default);
}
