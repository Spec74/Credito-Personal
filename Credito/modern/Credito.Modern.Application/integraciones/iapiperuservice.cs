namespace Credito.Modern.Application.Integraciones;

/// <summary>Proxy servidor para apiperu.dev (token no expuesto al navegador).</summary>
public interface IApiPeruService
{
    Task<ApiPeruDniDto> ConsultarDniAsync(string dni, CancellationToken cancellationToken = default);

    Task<ApiPeruRucDto> ConsultarRucAsync(string ruc, CancellationToken cancellationToken = default);
}
