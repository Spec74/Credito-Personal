namespace Credito.Modern.Application.Reportes;

public interface IReportesCatalogoCoberturaReadService
{
    Task<ReporteCatalogoCoberturaDto> ObtenerAsync(CancellationToken cancellationToken = default);
}
