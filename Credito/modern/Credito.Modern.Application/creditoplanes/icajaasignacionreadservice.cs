namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaAsignacionReadService
{
    Task<List<CajaParaAsignarRowDto>> ListarCajasParaAsignarAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saldo de la bóveda que usará <c>asignar-caja</c>: temporal si el usuario tiene
    /// <c>UsuarioRol.ENCARGADO</c> en la oficina; principal en caso contrario.
    /// </summary>
    Task<(decimal Monto, bool EsBovedaTemporal)> ObtenerMontoBovedaAsignacionAsync(
        int oficinaId,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
