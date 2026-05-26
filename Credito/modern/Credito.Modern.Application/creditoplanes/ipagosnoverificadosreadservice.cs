namespace Credito.Modern.Application.CreditoPlanes;

public interface IPagosNoVerificadosReadService
{
    /// <summary>Ejecuta <c>CREDITO.usp_PagosNoVerificados</c> sin parámetros.</summary>
    Task<IReadOnlyList<PagosNoVerificadosRowDto>> ListarAsync(CancellationToken cancellationToken = default);
}
