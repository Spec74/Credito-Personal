namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptSaldoCajaCabReadService
{
    /// <summary>Cabecera del reporte; <c>null</c> si la sesión de caja no existe.</summary>
    Task<RptSaldoCajaCabDto?> ObtenerAsync(
        int cajaDiarioId,
        bool cajaChica,
        CancellationToken cancellationToken = default);
}
