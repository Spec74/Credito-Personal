namespace Credito.Modern.Application.Reportes;

/// <summary>
/// Lista estática de acciones de informes legados (sin ejecutar RDLC).
/// </summary>
public interface IReportesCatalogoReadService
{
    Task<IReadOnlyList<ReporteCatalogoItemDto>> ListarAsync(CancellationToken cancellationToken = default);
}
