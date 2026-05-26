namespace Credito.Modern.Application.CreditoPlanes;

public interface ICentralRiesgoGenerarReadService
{
    Task<IReadOnlyList<CentralRiesgoGenerarRowDto>> ListarAsync(
        int oficinaId,
        int anio,
        int mes,
        CancellationToken cancellationToken = default);
}
