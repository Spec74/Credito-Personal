namespace Credito.Modern.Application.Reportes;

public interface IReportesExportPoliticaReadService
{
    Task<ReporteExportPoliticaDto> ObtenerAsync(CancellationToken cancellationToken = default);
}
