namespace Credito.Modern.Application.CreditoPlanes;

public interface ICuotasPendientesReadService
{
    /// <summary>Ejecuta <c>CREDITO.usp_CuotasPendientes</c> (misma firma que EF / <c>CajaDiarioBL</c>).</summary>
    Task<List<CuotasPendientesRowDto>> ListarAsync(
        int creditoId,
        DateTime fechaCalculo,
        bool indCancelacion,
        CancellationToken cancellationToken = default);
}
