using Credito.Modern.Application.Maestros;

namespace Credito.Modern.Application.CajaMaestro;

public interface ICajaMaestroReadService
{
    Task<CajaGestionPageDto> ListGestionAsync(
        string? buscar,
        int page,
        int pageSize,
        bool incluirInactivos,
        int? oficinaId = null,
        CancellationToken cancellationToken = default);

    Task<List<CajaGestorItemDto>> ListGestoresActivosAsync(
        int? oficinaId = null,
        CancellationToken cancellationToken = default);

    Task<List<CajaComboItemDto>> ListCajasActivasAsync(
        int? oficinaId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>CajaBL.Obtener(x =&gt; x.CajeroId == gestor)</c> en cobro diario MVC.</summary>
    Task<string?> GetDenominacionPorCajeroAsync(int cajeroId, CancellationToken cancellationToken = default);
}

public interface ICajaMaestroWriteService
{
    Task<MaestroOperacionResponse> GuardarAsync(
        GuardarCajaRequest request,
        int usuarioId,
        DateTime fechaServidor,
        int oficinaSesion,
        bool esCentral,
        CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> ActivarAsync(
        int cajaId,
        int usuarioId,
        DateTime fechaServidor,
        int oficinaSesion,
        bool esCentral,
        CancellationToken cancellationToken = default);
}
