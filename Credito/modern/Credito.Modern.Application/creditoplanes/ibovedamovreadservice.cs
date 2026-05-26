namespace Credito.Modern.Application.CreditoPlanes;

public interface IBovedaMovReadService
{
    Task<List<CajaAbiertaTransferenciaRowDto>> ListarCajasAbiertasParaTransferenciaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);
}
