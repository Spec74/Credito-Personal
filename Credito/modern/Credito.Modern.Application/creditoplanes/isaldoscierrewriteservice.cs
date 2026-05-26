namespace Credito.Modern.Application.CreditoPlanes;

public interface ISaldosCierreWriteService
{
    Task ActualizarDatosPostCierreBovedaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);
}
