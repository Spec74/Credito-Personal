namespace Credito.Modern.Application.CreditoPlanes;

public interface IPagosNoVerificadosReadService
{
    /// <summary>Ejecuta <c>CREDITO.usp_PagosNoVerificados</c> y restringe resultados a la oficina del token.</summary>
    Task<IReadOnlyList<PagosNoVerificadosRowDto>> ListarAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);
}
