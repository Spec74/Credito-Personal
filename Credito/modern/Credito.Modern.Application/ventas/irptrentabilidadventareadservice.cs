namespace Credito.Modern.Application.Ventas;

public interface IRptRentabilidadVentaReadService
{
    Task<IReadOnlyList<RptRentabilidadVentaRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        bool indContado,
        bool indCredito,
        int oficinaId,
        CancellationToken cancellationToken = default);
}
