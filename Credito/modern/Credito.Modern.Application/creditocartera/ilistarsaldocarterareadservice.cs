namespace Credito.Modern.Application.CreditoCartera;

public interface IListarSaldoCarteraReadService
{
    Task<List<ListarSaldoCarteraRowDto>> ListarAsync(
        int anio,
        int mes,
        int oficinaId,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
